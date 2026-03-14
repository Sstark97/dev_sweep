using AwesomeAssertions;
using DevSweep.Application.Models;
using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Common;
using DevSweep.Domain.Enums;
using DevSweep.Domain.ValueObjects;
using DevSweep.Infrastructure.Modules.Docker;
using DevSweep.Tests.Builders;
using NSubstitute;

namespace DevSweep.Tests.Infrastructure.Modules.Docker;

internal sealed class DockerCliRuntimeStrategyShould
{
    private readonly ICommandRunner commandRunner = Substitute.For<ICommandRunner>();
    private readonly IEnvironmentProvider environment = Substitute.For<IEnvironmentProvider>();
    private readonly DockerCliRuntimeStrategy strategy;

    private static readonly FilePath DockerConfig = FilePath.Create(Path.Combine("any", ".docker")).Value;

    public DockerCliRuntimeStrategyShould()
    {
        strategy = new DockerCliRuntimeStrategy(commandRunner, environment);
        environment.DockerConfigPath().Returns(DockerConfig);
    }

    // --- Availability ---

    [Test]
    public void BeAvailableOnMacOs()
    {
        strategy.IsAvailable(OperatingSystemType.MacOS).Should().BeTrue();
    }

    [Test]
    public void BeAvailableOnLinux()
    {
        strategy.IsAvailable(OperatingSystemType.Linux).Should().BeTrue();
    }

    [Test]
    public void BeAvailableOnWindows()
    {
        strategy.IsAvailable(OperatingSystemType.Windows).Should().BeTrue();
    }

    // --- Analysis: Docker not available ---

    [Test]
    public async Task FindNoItemsWhenDockerCliMissing()
    {
        GivenDockerCliNotAvailable();

        var result = await strategy.AnalyzeAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Test]
    public async Task FindNoItemsWhenDockerDaemonNotRunning()
    {
        GivenDockerCliAvailable();
        GivenDockerDaemonNotRunning();

        var result = await strategy.AnalyzeAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    // --- Analysis: safe candidates ---

    [Test]
    public async Task IncludeSafeCandidateWhenDockerAvailable()
    {
        GivenDockerAvailable();
        GivenNoResources();

        var result = await strategy.AnalyzeAsync(CancellationToken.None);

        var safeItems = result.Value.Where(i => i.IsSafeToDelete).ToList();
        result.IsSuccess.Should().BeTrue();
        safeItems.Should().HaveCount(1);
    }

    [Test]
    public async Task ReportPositiveSizeForSafeCandidateFromStoppedContainers()
    {
        GivenDockerAvailable();
        GivenStoppedContainerCount(3);
        GivenNoDanglingImages();
        GivenNoVolumes();
        GivenNoCustomNetworks();

        var result = await strategy.AnalyzeAsync(CancellationToken.None);

        var safeItem = result.Value.First(i => i.IsSafeToDelete);
        result.IsSuccess.Should().BeTrue();
        safeItem.Size.Should().BeGreaterThan(FileSize.Create(0).Value);
    }

    [Test]
    public async Task IncludeNetworksInSafeEstimationWhenCustomNetworksExist()
    {
        GivenDockerAvailable();
        GivenStoppedContainerCount(0);
        GivenNoDanglingImages();
        GivenNoVolumes();
        GivenCustomNetworkCount(2);

        var result = await strategy.AnalyzeAsync(CancellationToken.None);

        var safeItems = result.Value.Where(i => i.IsSafeToDelete).ToList();
        result.IsSuccess.Should().BeTrue();
        safeItems.Should().HaveCount(1);
    }

    [Test]
    public async Task ExcludeDefaultDockerNetworksFromEstimation()
    {
        GivenDockerAvailable();
        GivenStoppedContainerCount(0);
        GivenNoDanglingImages();
        GivenNoVolumes();
        commandRunner.RunAsync("docker", "network ls --format {{.Name}}", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandOutput.Create(0, "bridge\nhost\nnone", string.Empty)));

        var withDefaults = await strategy.AnalyzeAsync(CancellationToken.None);
        var sizeWithDefaults = withDefaults.Value.First(i => i.IsSafeToDelete).Size;

        GivenCustomNetworkCount(3);
        var withCustom = await strategy.AnalyzeAsync(CancellationToken.None);
        var sizeWithCustom = withCustom.Value.First(i => i.IsSafeToDelete).Size;

        withDefaults.IsSuccess.Should().BeTrue();
        withCustom.IsSuccess.Should().BeTrue();
        sizeWithCustom.Should().BeGreaterThan(sizeWithDefaults);
    }

    // --- Analysis: aggressive candidates ---

    [Test]
    public async Task AlwaysIncludeAggressiveCandidateAsUnsafeItem()
    {
        GivenDockerAvailable();
        GivenNoResources();

        var result = await strategy.AnalyzeAsync(CancellationToken.None);

        var unsafeItems = result.Value.Where(i => !i.IsSafeToDelete).ToList();
        result.IsSuccess.Should().BeTrue();
        unsafeItems.Should().NotBeEmpty();
    }

    [Test]
    public async Task ReportPositiveSizeForAggressiveCandidate()
    {
        GivenDockerAvailable();
        GivenNoResources();

        var result = await strategy.AnalyzeAsync(CancellationToken.None);

        var aggressiveItem = result.Value.First(i => !i.IsSafeToDelete);
        result.IsSuccess.Should().BeTrue();
        aggressiveItem.Size.Should().BeGreaterThan(FileSize.Create(0).Value);
    }

    // --- Cleanup: Docker not available ---

    [Test]
    public async Task ReturnEmptyWhenNoItemsProvided()
    {
        var result = await strategy.CleanAsync([], CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalFilesDeleted().Should().Be(0);
    }

    [Test]
    public async Task ReturnEmptyWhenDockerCliMissingDuringClean()
    {
        var safeItem = new CleanableItemBuilder().ForModule(CleanupModuleName.Docker).Build();
        GivenDockerCliNotAvailable();

        var result = await strategy.CleanAsync([safeItem], CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalFilesDeleted().Should().Be(0);
    }

    // --- Cleanup: safe path ---

    [Test]
    public async Task ExecuteSafePruneCommandWhenSafeItemsProvided()
    {
        var safeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .WithReason("docker:safe")
            .Large()
            .Build();
        GivenDockerCliAvailable();
        GivenDockerCommandSucceeds("system prune -f --volumes");

        await strategy.CleanAsync([safeItem], CancellationToken.None);

        await commandRunner.Received(1).RunAsync("docker", "system prune -f --volumes", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TrackFreedSpaceAfterSuccessfulSafePrune()
    {
        var safeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .WithReason("docker:safe")
            .Large()
            .Build();
        GivenDockerCliAvailable();
        GivenDockerCommandSucceeds("system prune -f --volumes");

        var result = await strategy.CleanAsync([safeItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(1);
        cleanupResult.TotalSpaceFreed().Should().Be(safeItem.Size);
    }

    [Test]
    public async Task RecordErrorWhenSafePruneCommandFails()
    {
        var safeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .WithReason("docker:safe")
            .Large()
            .Build();
        GivenDockerCliAvailable();
        GivenDockerCommandFails("system prune -f --volumes");

        var result = await strategy.CleanAsync([safeItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.HasErrors().Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(0);
    }

    // --- Cleanup: aggressive path ---

    [Test]
    public async Task ExecuteAggressivePruneCommandWhenUnsafeItemsProvided()
    {
        var unsafeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Unsafe()
            .WithReason("docker:aggressive")
            .Large()
            .Build();
        GivenDockerCliAvailable();
        GivenDockerCommandSucceeds("system prune -af --volumes");

        await strategy.CleanAsync([unsafeItem], CancellationToken.None);

        await commandRunner.Received(1).RunAsync("docker", "system prune -af --volumes", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TrackFreedSpaceAfterSuccessfulAggressivePrune()
    {
        var unsafeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Unsafe()
            .WithReason("docker:aggressive")
            .Large()
            .Build();
        GivenDockerCliAvailable();
        GivenDockerCommandSucceeds("system prune -af --volumes");

        var result = await strategy.CleanAsync([unsafeItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(1);
        cleanupResult.TotalSpaceFreed().Should().Be(unsafeItem.Size);
    }

    [Test]
    public async Task RecordErrorWhenAggressivePruneCommandFails()
    {
        var unsafeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Unsafe()
            .WithReason("docker:aggressive")
            .Large()
            .Build();
        GivenDockerCliAvailable();
        GivenDockerCommandFails("system prune -af --volumes");

        var result = await strategy.CleanAsync([unsafeItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.HasErrors().Should().BeTrue();
    }

    // --- Cleanup: partial failure handling ---

    [Test]
    public async Task ContinueWithAggressiveItemWhenSafePruneFails()
    {
        var safeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .WithReason("docker:safe")
            .Small()
            .Build();
        var unsafeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Unsafe()
            .WithReason("docker:aggressive")
            .Large()
            .Build();
        GivenDockerCliAvailable();
        GivenDockerCommandFails("system prune -f --volumes");
        GivenDockerCommandSucceeds("system prune -af --volumes");

        var result = await strategy.CleanAsync([safeItem, unsafeItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.HasErrors().Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(1);
    }

    [Test]
    public async Task AccumulateErrorsWhenBothPruneCommandsFail()
    {
        var safeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .WithReason("docker:safe")
            .Small()
            .Build();
        var unsafeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Unsafe()
            .WithReason("docker:aggressive")
            .Large()
            .Build();
        GivenDockerCliAvailable();
        GivenDockerCommandFails("system prune -f --volumes");
        GivenDockerCommandFails("system prune -af --volumes");

        var result = await strategy.CleanAsync([safeItem, unsafeItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.HasErrors().Should().BeTrue();
        cleanupResult.ErrorMessages().Should().HaveCount(2);
    }

    [Test]
    public async Task AccumulateFreedSpaceAcrossSafeAndAggressiveItems()
    {
        var safeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .WithReason("docker:safe")
            .Small()
            .Build();
        var unsafeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .Unsafe()
            .WithReason("docker:aggressive")
            .Large()
            .Build();
        var expectedTotal = safeItem.Size.Add(unsafeItem.Size);
        GivenDockerCliAvailable();
        GivenDockerCommandSucceeds("system prune -f --volumes");
        GivenDockerCommandSucceeds("system prune -af --volumes");

        var result = await strategy.CleanAsync([safeItem, unsafeItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(2);
        cleanupResult.TotalSpaceFreed().Should().Be(expectedTotal);
    }

    // --- Private helpers ---

    private void GivenDockerCliAvailable() =>
        commandRunner.IsCommandAvailable("docker").Returns(true);

    private void GivenDockerCliNotAvailable() =>
        commandRunner.IsCommandAvailable("docker").Returns(false);

    private void GivenDockerDaemonNotRunning() =>
        commandRunner.RunAsync("docker", "info", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandOutput.Create(1, string.Empty, "Cannot connect to the Docker daemon")));

    private void GivenDockerAvailable()
    {
        GivenDockerCliAvailable();
        commandRunner.RunAsync("docker", "info", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandOutput.Create(0, "docker info output", string.Empty)));
    }

    private void GivenStoppedContainerCount(int count)
    {
        var output = string.Join("\n", Enumerable.Repeat("abc123", count));
        commandRunner.RunAsync("docker", "ps -aq --filter status=exited", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandOutput.Create(0, output, string.Empty)));
    }

    private void GivenNoDanglingImages() =>
        commandRunner.RunAsync("docker", "images -f dangling=true -q", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandOutput.Create(0, string.Empty, string.Empty)));

    private void GivenNoVolumes() =>
        commandRunner.RunAsync("docker", "volume ls -q", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandOutput.Create(0, string.Empty, string.Empty)));

    private void GivenNoCustomNetworks() =>
        commandRunner.RunAsync("docker", "network ls --format {{.Name}}", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandOutput.Create(0, "bridge\nhost\nnone", string.Empty)));

    private void GivenCustomNetworkCount(int count)
    {
        var customNetworks = string.Join("\n", Enumerable.Repeat("my-custom-net", count));
        commandRunner.RunAsync("docker", "network ls --format {{.Name}}", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandOutput.Create(0, $"bridge\nhost\nnone\n{customNetworks}", string.Empty)));
    }

    private void GivenNoResources()
    {
        GivenStoppedContainerCount(0);
        GivenNoDanglingImages();
        GivenNoVolumes();
        GivenNoCustomNetworks();
    }

    private void GivenDockerCommandSucceeds(string arguments) =>
        commandRunner.RunAsync("docker", arguments, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandOutput.Create(0, "Successfully pruned", string.Empty)));

    private void GivenDockerCommandFails(string arguments) =>
        commandRunner.RunAsync("docker", arguments, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandOutput.Create(1, string.Empty, "Error response from daemon")));
}
