using AwesomeAssertions;
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

internal sealed class OrbStackRuntimeStrategyShould
{
    private readonly ICommandRunner commandRunner = Substitute.For<ICommandRunner>();
    private readonly IFileSystem fileSystem = Substitute.For<IFileSystem>();
    private readonly IProcessManager processManager = Substitute.For<IProcessManager>();
    private readonly IEnvironmentProvider environment = Substitute.For<IEnvironmentProvider>();
    private readonly OrbStackRuntimeStrategy strategy;

    private static readonly FilePath Home = FilePath.Create(Path.Combine("any", "home")).Value;
    private static readonly FilePath OrbDir = FilePath.Create(Path.Combine("any", "home", ".orbstack")).Value;
    private static readonly FilePath CacheDir = FilePath.Create(Path.Combine("any", "home", ".orbstack", "cache")).Value;

    public OrbStackRuntimeStrategyShould()
    {
        strategy = new OrbStackRuntimeStrategy(commandRunner, fileSystem, processManager, environment);
        environment.HomePath.Returns(Home);
    }

    // --- Availability ---

    [Test]
    public void BeAvailableOnMacOs()
    {
        strategy.IsAvailable(OperatingSystemType.MacOS).Should().BeTrue();
    }

    [Test]
    public void BeUnavailableOnLinux()
    {
        strategy.IsAvailable(OperatingSystemType.Linux).Should().BeFalse();
    }

    [Test]
    public void BeUnavailableOnWindows()
    {
        strategy.IsAvailable(OperatingSystemType.Windows).Should().BeFalse();
    }

    // --- Analysis: OrbStack not installed ---

    [Test]
    public async Task FindNoItemsWhenOrbStackNotInstalled()
    {
        GivenOrbStackDirMissing();
        GivenOrbctlNotAvailable();

        var result = await strategy.AnalyzeAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    // --- Analysis: detection methods ---

    [Test]
    public async Task DetectOrbStackViaDirectoryPresence()
    {
        GivenOrbStackDirPresent();
        GivenCacheDirPresentAndNotEmpty();

        var result = await strategy.AnalyzeAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Test]
    public async Task DetectOrbStackViaOrbctlCommand()
    {
        GivenOrbStackDirMissing();
        GivenOrbctlAvailable();
        GivenCacheDirPresentAndNotEmpty();

        var result = await strategy.AnalyzeAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    // --- Analysis: cache presence ---

    [Test]
    public async Task FindCacheItemWhenCacheDirExistsAndNotEmpty()
    {
        GivenOrbStackDirPresent();
        GivenCacheDirPresentAndNotEmpty();

        var result = await strategy.AnalyzeAsync(CancellationToken.None);

        var item = result.Value.Single();
        result.IsSuccess.Should().BeTrue();
        item.IsSafeToDelete.Should().BeFalse();
        item.Reason.Should().Be("docker:orbstack-cache");
    }

    [Test]
    public async Task FindNoItemsWhenCacheDirMissing()
    {
        GivenOrbStackDirPresent();
        fileSystem.DirectoryExists(CacheDir).Returns(false);

        var result = await strategy.AnalyzeAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Test]
    public async Task FindNoItemsWhenCacheDirIsEmpty()
    {
        GivenOrbStackDirPresent();
        fileSystem.DirectoryExists(CacheDir).Returns(true);
        fileSystem.IsDirectoryNotEmpty(CacheDir).Returns(false);

        var result = await strategy.AnalyzeAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Test]
    public async Task ReportActualCacheSizeFromFileSystem()
    {
        var expectedSize = FileSize.Create(5L * 1024L * 1024L * 1024L).Value;
        GivenOrbStackDirPresent();
        fileSystem.DirectoryExists(CacheDir).Returns(true);
        fileSystem.IsDirectoryNotEmpty(CacheDir).Returns(true);
        fileSystem.Size(CacheDir).Returns(Result<FileSize, DomainError>.Success(expectedSize));

        var result = await strategy.AnalyzeAsync(CancellationToken.None);

        var item = result.Value.Single();
        result.IsSuccess.Should().BeTrue();
        item.Size.Should().Be(expectedSize);
    }

    // --- Cleanup: no OrbStack items ---

    [Test]
    public async Task ReturnEmptyWhenNoOrbStackItemsProvided()
    {
        var dockerItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Docker)
            .WithReason("docker:safe")
            .Build();

        var result = await strategy.CleanAsync([dockerItem], CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalFilesDeleted().Should().Be(0);
    }

    [Test]
    public async Task ReturnEmptyWhenNoItemsAtAll()
    {
        var result = await strategy.CleanAsync([], CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalFilesDeleted().Should().Be(0);
    }

    // --- Cleanup: process lifecycle ---

    [Test]
    public async Task SkipStopWhenOrbStackNotRunning()
    {
        processManager.IsProcessRunning("OrbStack").Returns(false);
        fileSystem.DirectoryExists(CacheDir).Returns(true);
        fileSystem.DeleteDirectoryAsync(CacheDir, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Unit, DomainError>.Success(Unit.Value)));

        await strategy.CleanAsync([GivenOrbStackItem()], CancellationToken.None);

        await processManager.DidNotReceive().KillProcessAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StopOrbStackProcessBeforeCleaningCache()
    {
        processManager.IsProcessRunning("OrbStack").Returns(true);
        processManager.KillProcessAsync("OrbStack", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<bool, DomainError>.Success(true)));
        fileSystem.DirectoryExists(CacheDir).Returns(true);
        fileSystem.DeleteDirectoryAsync(CacheDir, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Unit, DomainError>.Success(Unit.Value)));

        await strategy.CleanAsync([GivenOrbStackItem()], CancellationToken.None);

        await processManager.Received(1).KillProcessAsync("OrbStack", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RecordErrorWhenProcessKillFails()
    {
        processManager.IsProcessRunning("OrbStack").Returns(true);
        processManager.KillProcessAsync("OrbStack", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<bool, DomainError>.Failure(DomainError.InvalidOperation("Access denied"))));

        var result = await strategy.CleanAsync([GivenOrbStackItem()], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.HasErrors().Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(0);
    }

    // --- Cleanup: cache deletion ---

    [Test]
    public async Task DeleteCacheDirectoryDuringClean()
    {
        processManager.IsProcessRunning("OrbStack").Returns(false);
        fileSystem.DirectoryExists(CacheDir).Returns(true);
        fileSystem.DeleteDirectoryAsync(CacheDir, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Unit, DomainError>.Success(Unit.Value)));

        var result = await strategy.CleanAsync([GivenOrbStackItem()], CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await fileSystem.Received(1).DeleteDirectoryAsync(CacheDir, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TrackDeletedItemAfterSuccessfulClean()
    {
        processManager.IsProcessRunning("OrbStack").Returns(false);
        fileSystem.DirectoryExists(CacheDir).Returns(true);
        fileSystem.DeleteDirectoryAsync(CacheDir, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Unit, DomainError>.Success(Unit.Value)));

        var result = await strategy.CleanAsync([GivenOrbStackItem()], CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalFilesDeleted().Should().Be(1);
    }

    [Test]
    public async Task SkipDeletionWhenCacheDirMissing()
    {
        processManager.IsProcessRunning("OrbStack").Returns(false);
        fileSystem.DirectoryExists(CacheDir).Returns(false);

        var result = await strategy.CleanAsync([GivenOrbStackItem()], CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalFilesDeleted().Should().Be(1);
        await fileSystem.DidNotReceive().DeleteDirectoryAsync(Arg.Any<FilePath>(), Arg.Any<CancellationToken>());
    }

    // --- Private helpers ---

    private void GivenOrbStackDirPresent() =>
        fileSystem.DirectoryExists(OrbDir).Returns(true);

    private void GivenOrbStackDirMissing() =>
        fileSystem.DirectoryExists(OrbDir).Returns(false);

    private void GivenOrbctlAvailable() =>
        commandRunner.IsCommandAvailable("orbctl").Returns(true);

    private void GivenOrbctlNotAvailable() =>
        commandRunner.IsCommandAvailable("orbctl").Returns(false);

    private void GivenCacheDirPresentAndNotEmpty()
    {
        fileSystem.DirectoryExists(CacheDir).Returns(true);
        fileSystem.IsDirectoryNotEmpty(CacheDir).Returns(true);
        fileSystem.Size(CacheDir).Returns(Result<FileSize, DomainError>.Success(FileSize.Create(1024).Value));
    }

    private static CleanableItem GivenOrbStackItem() =>
        new CleanableItemBuilder()
            .WithPath(Path.Combine("any", "home", ".orbstack", "cache"))
            .ForModule(CleanupModuleName.Docker)
            .Unsafe()
            .WithReason("docker:orbstack-cache")
            .Large()
            .Build();
}
