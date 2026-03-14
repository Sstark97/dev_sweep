using AwesomeAssertions;
using DevSweep.Application.Models;
using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Common;
using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;
using DevSweep.Infrastructure.Modules.Docker;
using DevSweep.Tests.Builders;
using NSubstitute;

namespace DevSweep.Tests.Infrastructure.Modules.Docker;

internal sealed class DockerModuleShould
{
    private readonly IContainerRuntimeStrategy dockerStrategy = Substitute.For<IContainerRuntimeStrategy>();
    private readonly IContainerRuntimeStrategy orbStrategy = Substitute.For<IContainerRuntimeStrategy>();
    private readonly IEnvironmentProvider environment = Substitute.For<IEnvironmentProvider>();
    private readonly DockerModule module;

    public DockerModuleShould()
    {
        environment.CurrentOperatingSystem.Returns(OperatingSystemType.MacOS);

        dockerStrategy.RuntimeName.Returns("Docker CLI");
        dockerStrategy.IsAvailable(Arg.Any<OperatingSystemType>()).Returns(true);
        dockerStrategy.AnalyzeAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CleanableItem>, DomainError>.Success([])));
        dockerStrategy.CleanAsync(Arg.Any<IReadOnlyList<CleanableItem>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<CleanupResult, DomainError>.Success(CleanupResult.Empty)));

        orbStrategy.RuntimeName.Returns("OrbStack");
        orbStrategy.IsAvailable(OperatingSystemType.MacOS).Returns(true);
        orbStrategy.IsAvailable(OperatingSystemType.Linux).Returns(false);
        orbStrategy.IsAvailable(OperatingSystemType.Windows).Returns(false);
        orbStrategy.AnalyzeAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CleanableItem>, DomainError>.Success([])));
        orbStrategy.CleanAsync(Arg.Any<IReadOnlyList<CleanableItem>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<CleanupResult, DomainError>.Success(CleanupResult.Empty)));

        module = new DockerModule(environment, [dockerStrategy, orbStrategy]);
    }

    // --- Platform availability ---

    [Test]
    public void BeAvailableOnMacOs()
    {
        module.IsAvailableOnPlatform(OperatingSystemType.MacOS).Should().BeTrue();
    }

    [Test]
    public void BeAvailableOnLinux()
    {
        module.IsAvailableOnPlatform(OperatingSystemType.Linux).Should().BeTrue();
    }

    [Test]
    public void BeAvailableOnWindows()
    {
        module.IsAvailableOnPlatform(OperatingSystemType.Windows).Should().BeTrue();
    }

    [Test]
    public void BeUnavailableOnUnknownPlatform()
    {
        module.IsAvailableOnPlatform((OperatingSystemType)999).Should().BeFalse();
    }

    [Test]
    public void BeDestructive()
    {
        module.IsDestructive.Should().BeTrue();
    }

    // --- Analysis: orchestration ---

    [Test]
    public async Task CombineItemsFromAllActiveStrategiesDuringAnalysis()
    {
        var dockerItem = new CleanableItemBuilder().ForModule(CleanupModuleName.Docker).WithReason("docker:safe").Build();
        var orbItem = new CleanableItemBuilder().ForModule(CleanupModuleName.Docker).Unsafe().WithReason("docker:orbstack-cache").Build();
        dockerStrategy.AnalyzeAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CleanableItem>, DomainError>.Success([dockerItem])));
        orbStrategy.AnalyzeAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CleanableItem>, DomainError>.Success([orbItem])));

        var result = await module.AnalyzeAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ItemCount().Should().Be(2);
    }

    [Test]
    public async Task ReturnEmptyAnalysisWhenNoStrategiesHaveItems()
    {
        var result = await module.AnalyzeAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ItemCount().Should().Be(0);
    }

    [Test]
    public async Task SkipStrategyUnavailableForCurrentOsDuringAnalysis()
    {
        environment.CurrentOperatingSystem.Returns(OperatingSystemType.Linux);
        var orbItem = new CleanableItemBuilder().ForModule(CleanupModuleName.Docker).Unsafe().WithReason("docker:orbstack-cache").Build();
        orbStrategy.AnalyzeAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CleanableItem>, DomainError>.Success([orbItem])));

        var result = await module.AnalyzeAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await orbStrategy.DidNotReceive().AnalyzeAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ContinueWithRemainingStrategiesWhenOneFailsDuringAnalysis()
    {
        var dockerItem = new CleanableItemBuilder().ForModule(CleanupModuleName.Docker).WithReason("docker:safe").Build();
        dockerStrategy.AnalyzeAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CleanableItem>, DomainError>.Failure(DomainError.InvalidOperation("Unexpected error"))));
        orbStrategy.AnalyzeAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CleanableItem>, DomainError>.Success([dockerItem])));

        var result = await module.AnalyzeAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ItemCount().Should().Be(1);
    }

    // --- Clean: orchestration ---

    [Test]
    public async Task ReturnEmptyWhenNoItemsProvided()
    {
        var result = await module.CleanAsync([], CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalFilesDeleted().Should().Be(0);
    }

    [Test]
    public async Task DelegateCleanToAllActiveStrategies()
    {
        var item = new CleanableItemBuilder().ForModule(CleanupModuleName.Docker).Build();

        await module.CleanAsync([item], CancellationToken.None);

        await dockerStrategy.Received(1).CleanAsync(Arg.Any<IReadOnlyList<CleanableItem>>(), Arg.Any<CancellationToken>());
        await orbStrategy.Received(1).CleanAsync(Arg.Any<IReadOnlyList<CleanableItem>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CombineCleanupResultsFromAllStrategies()
    {
        var item = new CleanableItemBuilder().ForModule(CleanupModuleName.Docker).Large().Build();
        dockerStrategy.CleanAsync(Arg.Any<IReadOnlyList<CleanableItem>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CleanupResult.Create(1, FileSize.Create(2048).Value)));
        orbStrategy.CleanAsync(Arg.Any<IReadOnlyList<CleanableItem>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CleanupResult.Create(1, FileSize.Create(1024).Value)));

        var result = await module.CleanAsync([item], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(2);
    }

    [Test]
    public async Task SkipStrategyUnavailableForCurrentOsDuringClean()
    {
        environment.CurrentOperatingSystem.Returns(OperatingSystemType.Linux);
        var item = new CleanableItemBuilder().ForModule(CleanupModuleName.Docker).Build();

        await module.CleanAsync([item], CancellationToken.None);

        await orbStrategy.DidNotReceive().CleanAsync(Arg.Any<IReadOnlyList<CleanableItem>>(), Arg.Any<CancellationToken>());
        await dockerStrategy.Received(1).CleanAsync(Arg.Any<IReadOnlyList<CleanableItem>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AccumulateErrorsAcrossStrategies()
    {
        var item = new CleanableItemBuilder().ForModule(CleanupModuleName.Docker).Build();
        dockerStrategy.CleanAsync(Arg.Any<IReadOnlyList<CleanableItem>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CleanupResult.CreateWithErrors(0, FileSize.Zero, ["Docker prune failed"])));
        orbStrategy.CleanAsync(Arg.Any<IReadOnlyList<CleanableItem>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CleanupResult.CreateWithErrors(0, FileSize.Zero, ["OrbStack cleanup failed"])));

        var result = await module.CleanAsync([item], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.HasErrors().Should().BeTrue();
        cleanupResult.ErrorMessages().Should().HaveCount(2);
    }
}
