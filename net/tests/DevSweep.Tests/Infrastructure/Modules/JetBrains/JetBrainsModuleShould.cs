using AwesomeAssertions;
using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Common;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;
using DevSweep.Infrastructure.Modules.JetBrains;
using DevSweep.Tests.Builders;
using NSubstitute;

namespace DevSweep.Tests.Infrastructure.Modules.JetBrains;

internal sealed class JetBrainsModuleShould
{
    private readonly IFileSystem fileSystem = Substitute.For<IFileSystem>();
    private readonly IProcessManager processManager = Substitute.For<IProcessManager>();
    private readonly IEnvironmentProvider environment = Substitute.For<IEnvironmentProvider>();
    private readonly JetBrainsModule module;

    private static readonly FilePath BasePath = FilePath.Create(Path.Combine("any", "base", "path")).Value;
    private static readonly FilePath CachePath = FilePath.Create(Path.Combine("any", "cache", "path")).Value;

    public JetBrainsModuleShould()
    {
        module = new JetBrainsModule(fileSystem, processManager, environment);
        environment.JetBrainsBasePath().Returns(BasePath);
        environment.JetBrainsCachePath().Returns(CachePath);
    }

    // --- Platform availability ---

    [Test]
    public void MarkOperationAsSafe()
    {
        module.IsDestructive.Should().BeFalse();
    }

    [Test]
    public void BeAvailableWhenPlatformIsMacOs()
    {
        module.IsAvailableOnPlatform(OperatingSystemType.MacOS).Should().BeTrue();
    }

    [Test]
    public void BeAvailableWhenPlatformIsLinux()
    {
        module.IsAvailableOnPlatform(OperatingSystemType.Linux).Should().BeTrue();
    }

    [Test]
    public void BeAvailableWhenPlatformIsWindows()
    {
        module.IsAvailableOnPlatform(OperatingSystemType.Windows).Should().BeTrue();
    }

    [Test]
    public void BeUnavailableWhenPlatformIsUnknown()
    {
        module.IsAvailableOnPlatform((OperatingSystemType)999).Should().BeFalse();
    }

    [Test]
    public async Task FindNoItemsWhenBaseDirectoryMissing()
    {
        fileSystem.DirectoryExists(BasePath).Returns(false);

        var result = await module.AnalyzeAsync(CancellationToken.None);

        var analysis = result.Value;
        result.IsSuccess.Should().BeTrue();
        analysis.ItemCount().Should().Be(0);
    }

    [Test]
    public async Task FindNoItemsWhenSingleVersionInstalled()
    {
        var rider = FilePath.Create(Path.Combine("any", "base", "path", "Rider2024.1")).Value;
        GivenBaseDirectoryContains(rider);

        var result = await module.AnalyzeAsync(CancellationToken.None);

        var analysis = result.Value;
        result.IsSuccess.Should().BeTrue();
        analysis.ItemCount().Should().Be(0);
    }

    [Test]
    public async Task FlagOlderVersionForCleanupWhenTwoVersionsInstalled()
    {
        var olderRider = FilePath.Create(Path.Combine("any", "base", "path", "Rider2023.3")).Value;
        var newerRider = FilePath.Create(Path.Combine("any", "base", "path", "Rider2024.1")).Value;
        var smallSize = new CleanableItemBuilder().Small().Build().Size;
        GivenBaseDirectoryContains(olderRider, newerRider);
        GivenDirectoryHasSize(olderRider, smallSize);

        var result = await module.AnalyzeAsync(CancellationToken.None);

        var items = result.Value.Items();
        result.IsSuccess.Should().BeTrue();
        items.Should().HaveCount(1);
        items[0].Path.Should().Be(olderRider);
        items[0].IsSafeToDelete.Should().BeTrue();
    }

    [Test]
    public async Task FlagAllButLatestForCleanupWhenMultipleVersionsInstalled()
    {
        var earliestRider = FilePath.Create(Path.Combine("any", "base", "path", "Rider2022.3")).Value;
        var middleRider = FilePath.Create(Path.Combine("any", "base", "path", "Rider2023.3")).Value;
        var latestRider = FilePath.Create(Path.Combine("any", "base", "path", "Rider2024.1")).Value;
        var smallSize = new CleanableItemBuilder().Small().Build().Size;
        GivenBaseDirectoryContains(earliestRider, middleRider, latestRider);
        GivenAllDirectoriesHaveSize(smallSize);

        var result = await module.AnalyzeAsync(CancellationToken.None);

        var items = result.Value.Items();
        result.IsSuccess.Should().BeTrue();
        items.Should().HaveCount(2);
        items.Should().NotContain(i => i.Path.Equals(latestRider));
    }

    [Test]
    public async Task SortVersionsNaturallyAcrossSegments()
    {
        var patchVersion = FilePath.Create(Path.Combine("any", "base", "path", "Rider2023.3.1")).Value;
        var minorVersion = FilePath.Create(Path.Combine("any", "base", "path", "Rider2024.1")).Value;
        var smallSize = new CleanableItemBuilder().Small().Build().Size;
        GivenBaseDirectoryContains(minorVersion, patchVersion);
        GivenDirectoryHasSize(patchVersion, smallSize);

        var result = await module.AnalyzeAsync(CancellationToken.None);

        var items = result.Value.Items();
        result.IsSuccess.Should().BeTrue();
        items.Should().HaveCount(1);
        items[0].Path.Should().Be(patchVersion);
    }

    [Test]
    public async Task FlagOneOutdatedVersionPerProductLineIndependently()
    {
        var olderRider = FilePath.Create(Path.Combine("any", "base", "path", "Rider2023.3")).Value;
        var newerRider = FilePath.Create(Path.Combine("any", "base", "path", "Rider2024.1")).Value;
        var olderWebStorm = FilePath.Create(Path.Combine("any", "base", "path", "WebStorm2023.3")).Value;
        var newerWebStorm = FilePath.Create(Path.Combine("any", "base", "path", "WebStorm2024.1")).Value;
        var smallSize = new CleanableItemBuilder().Small().Build().Size;
        GivenBaseDirectoryContains(olderRider, newerRider, olderWebStorm, newerWebStorm);
        GivenAllDirectoriesHaveSize(smallSize);

        var result = await module.AnalyzeAsync(CancellationToken.None);

        var analysis = result.Value;
        result.IsSuccess.Should().BeTrue();
        analysis.ItemCount().Should().Be(2);
    }

    [Test]
    public async Task IncludeCacheDirectoryWhenNonEmpty()
    {
        var largeSize = new CleanableItemBuilder().Large().Build().Size;
        GivenBaseDirectoryContains();
        GivenCacheDirectoryIsNonEmpty(largeSize);

        var result = await module.AnalyzeAsync(CancellationToken.None);

        var items = result.Value.Items();
        result.IsSuccess.Should().BeTrue();
        items.Should().HaveCount(1);
        items[0].Path.Should().Be(CachePath);
    }

    [Test]
    public async Task SkipCacheDirectoryWhenMissing()
    {
        GivenBaseDirectoryContains();

        var result = await module.AnalyzeAsync(CancellationToken.None);

        var analysis = result.Value;
        result.IsSuccess.Should().BeTrue();
        analysis.ItemCount().Should().Be(0);
    }

    [Test]
    public async Task SkipCacheDirectoryWhenEmpty()
    {
        GivenBaseDirectoryContains();
        fileSystem.DirectoryExists(CachePath).Returns(true);
        fileSystem.IsDirectoryNotEmpty(CachePath).Returns(false);

        var result = await module.AnalyzeAsync(CancellationToken.None);

        var analysis = result.Value;
        result.IsSuccess.Should().BeTrue();
        analysis.ItemCount().Should().Be(0);
    }

    [Test]
    public async Task ExcludeNestedSubdirectoriesFromVersionDetection()
    {
        var immediateChild = FilePath.Create(Path.Combine("any", "base", "path", "Rider2024.1")).Value;
        var nestedChild = FilePath.Create(Path.Combine("any", "base", "path", "Rider2024.1", "plugins", "SomePlugin")).Value;
        GivenBaseDirectoryContains(immediateChild, nestedChild);

        var result = await module.AnalyzeAsync(CancellationToken.None);

        var analysis = result.Value;
        result.IsSuccess.Should().BeTrue();
        analysis.ItemCount().Should().Be(0);
    }

    // --- Analysis: failure propagation ---

    [Test]
    public async Task FailAnalysisWhenDirectorySizeCantBeRead()
    {
        var olderRider = FilePath.Create(Path.Combine("any", "base", "path", "Rider2023.3")).Value;
        var newerRider = FilePath.Create(Path.Combine("any", "base", "path", "Rider2024.1")).Value;
        GivenBaseDirectoryContains(olderRider, newerRider);
        fileSystem.Size(olderRider).Returns(
            Result<FileSize, DomainError>.Failure(DomainError.InvalidOperation("Cannot read size")));

        var result = await module.AnalyzeAsync(CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task FailAnalysisWhenDirectorySearchFails()
    {
        fileSystem.DirectoryExists(BasePath).Returns(true);
        fileSystem.FindDirectoriesAsync(BasePath, "*", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                Result<IReadOnlyList<FilePath>, DomainError>.Failure(
                    DomainError.InvalidOperation("Permission denied"))));

        var result = await module.AnalyzeAsync(CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // --- Cleanup: process management ---

    [Test]
    public async Task ShutDownRunningIdeBeforeDeletion()
    {
        GivenProcessRunningOnly("Rider");
        GivenProcessKillSucceeds("Rider");

        await module.CleanAsync([], CancellationToken.None);

        await processManager.Received(1).KillProcessAsync("Rider", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SkipProcessTerminationWhenNoIdeRunning()
    {
        GivenNoProcessesRunning();

        await module.CleanAsync([], CancellationToken.None);

        await processManager.DidNotReceive().KillProcessAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RecordKillErrorWithoutAbortingCleanup()
    {
        GivenProcessRunningOnly("Rider");
        GivenProcessKillFails("Rider", "Access denied");

        var result = await module.CleanAsync([], CancellationToken.None);

        var cleanupResult = result.Value;
        var errorMessages = cleanupResult.ErrorMessages();
        result.IsSuccess.Should().BeTrue();
        cleanupResult.HasErrors().Should().BeTrue();
        errorMessages.Should().HaveCount(1);
    }

    [Test]
    public async Task DeleteSafeItemAndTrackFreedSpace()
    {
        var item = new CleanableItemBuilder()
            .WithPath(Path.Combine("any", "base", "path", "Rider2023.3"))
            .ForModule(CleanupModuleName.JetBrains)
            .WithReason("outdated")
            .Large()
            .Build();
        GivenNoProcessesRunning();
        GivenDeleteSucceeds(item.Path);

        var result = await module.CleanAsync([item], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(1);
        cleanupResult.TotalSpaceFreed().Should().Be(item.Size);
    }

    [Test]
    public async Task DeleteNothingWhenItemListIsEmpty()
    {
        GivenNoProcessesRunning();

        var result = await module.CleanAsync([], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(0);
    }

    [Test]
    public async Task SkipDeletionForUnsafeItems()
    {
        var unsafeItem = new CleanableItemBuilder()
            .WithPath(Path.Combine("any", "base", "path", "Rider2024.1"))
            .ForModule(CleanupModuleName.JetBrains)
            .Unsafe()
            .Build();
        GivenNoProcessesRunning();

        var result = await module.CleanAsync([unsafeItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(0);
        await fileSystem.DidNotReceive().DeleteDirectoryAsync(Arg.Any<FilePath>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ContinueDeletingRemainingItemsWhenOneDeletionFails()
    {
        var failItem = new CleanableItemBuilder()
            .WithPath(Path.Combine("any", "base", "path", "Rider2022.3"))
            .ForModule(CleanupModuleName.JetBrains)
            .WithReason("outdated")
            .Small()
            .Build();
        var successItem = new CleanableItemBuilder()
            .WithPath(Path.Combine("any", "base", "path", "Rider2023.3"))
            .ForModule(CleanupModuleName.JetBrains)
            .WithReason("outdated")
            .Large()
            .Build();
        GivenNoProcessesRunning();
        GivenDeleteFails(failItem.Path, "Locked");
        GivenDeleteSucceeds(successItem.Path);

        var result = await module.CleanAsync([failItem, successItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(1);
    }

    [Test]
    public async Task RecordDeletionErrorsWithoutAbortingCleanup()
    {
        var failItem = new CleanableItemBuilder()
            .WithPath(Path.Combine("any", "base", "path", "Rider2022.3"))
            .ForModule(CleanupModuleName.JetBrains)
            .WithReason("outdated")
            .Small()
            .Build();
        var successItem = new CleanableItemBuilder()
            .WithPath(Path.Combine("any", "base", "path", "Rider2023.3"))
            .ForModule(CleanupModuleName.JetBrains)
            .WithReason("outdated")
            .Large()
            .Build();
        GivenNoProcessesRunning();
        GivenDeleteFails(failItem.Path, "Locked");
        GivenDeleteSucceeds(successItem.Path);

        var result = await module.CleanAsync([failItem, successItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.HasErrors().Should().BeTrue();
        cleanupResult.ErrorMessages().Should().HaveCount(1);
    }

    [Test]
    public async Task AccumulateFreedSpaceAcrossDeletedItems()
    {
        var olderRiderItem = new CleanableItemBuilder()
            .WithPath(Path.Combine("any", "base", "path", "Rider2022.3"))
            .ForModule(CleanupModuleName.JetBrains)
            .WithReason("outdated")
            .Small()
            .Build();
        var newerRiderItem = new CleanableItemBuilder()
            .WithPath(Path.Combine("any", "base", "path", "Rider2023.3"))
            .ForModule(CleanupModuleName.JetBrains)
            .WithReason("outdated")
            .Large()
            .Build();
        var expectedTotal = olderRiderItem.Size.Add(newerRiderItem.Size);
        GivenNoProcessesRunning();
        GivenDeleteSucceeds();

        var result = await module.CleanAsync([olderRiderItem, newerRiderItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(2);
        cleanupResult.TotalSpaceFreed().Should().Be(expectedTotal);
    }

    [Test]
    public async Task IgnoreSingleVersionProductWhenCoexistingWithMultiVersionProduct()
    {
        var riderOld = FilePath.Create(Path.Combine("any", "base", "path", "Rider2023.3")).Value;
        var riderNew = FilePath.Create(Path.Combine("any", "base", "path", "Rider2024.1")).Value;
        var webStormSingle = FilePath.Create(Path.Combine("any", "base", "path", "WebStorm2024.1")).Value;
        var smallSize = new CleanableItemBuilder().Small().Build().Size;
        GivenBaseDirectoryContains(riderOld, riderNew, webStormSingle);
        GivenAllDirectoriesHaveSize(smallSize);

        var result = await module.AnalyzeAsync(CancellationToken.None);

        var items = result.Value.Items();
        result.IsSuccess.Should().BeTrue();
        items.Should().HaveCount(1);
        items[0].Path.Should().Be(riderOld);
    }

    private void GivenBaseDirectoryContains(params FilePath[] directories)
    {
        fileSystem.DirectoryExists(BasePath).Returns(true);
        fileSystem.FindDirectoriesAsync(BasePath, "*", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                Result<IReadOnlyList<FilePath>, DomainError>.Success(directories)));
        fileSystem.DirectoryExists(CachePath).Returns(false);
    }

    private void GivenDirectoryHasSize(FilePath path, FileSize size)
    {
        fileSystem.Size(path).Returns(Result<FileSize, DomainError>.Success(size));
    }

    private void GivenAllDirectoriesHaveSize(FileSize size)
    {
        fileSystem.Size(Arg.Any<FilePath>()).Returns(Result<FileSize, DomainError>.Success(size));
    }

    private void GivenCacheDirectoryIsNonEmpty(FileSize size)
    {
        fileSystem.DirectoryExists(CachePath).Returns(true);
        fileSystem.IsDirectoryNotEmpty(CachePath).Returns(true);
        fileSystem.Size(CachePath).Returns(Result<FileSize, DomainError>.Success(size));
    }

    private void GivenNoProcessesRunning()
    {
        processManager.IsProcessRunning(Arg.Any<string>()).Returns(false);
    }

    private void GivenDeleteSucceeds()
    {
        fileSystem.DeleteDirectoryAsync(Arg.Any<FilePath>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Unit, DomainError>.Success(Unit.Value)));
    }

    private void GivenDeleteSucceeds(FilePath path)
    {
        fileSystem.DeleteDirectoryAsync(path, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Unit, DomainError>.Success(Unit.Value)));
    }

    private void GivenDeleteFails(FilePath path, string reason)
    {
        fileSystem.DeleteDirectoryAsync(path, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                Result<Unit, DomainError>.Failure(DomainError.InvalidOperation(reason))));
    }

    private (FilePath older, FilePath newer) GivenTwoVersionsOfProduct(string product)
    {
        var older = FilePath.Create(Path.Combine("any", "base", "path", $"{product}2023.3")).Value;
        var newer = FilePath.Create(Path.Combine("any", "base", "path", $"{product}2024.1")).Value;
        GivenBaseDirectoryContains(older, newer);
        GivenAllDirectoriesHaveSize(new CleanableItemBuilder().Small().Build().Size);
        return (older, newer);
    }

    private void GivenProcessRunningOnly(string processName)
    {
        processManager.IsProcessRunning(processName).Returns(true);
        processManager.IsProcessRunning(Arg.Is<string>(s => s != processName)).Returns(false);
    }

    private void GivenProcessKillSucceeds(string processName)
    {
        processManager.KillProcessAsync(processName, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<bool, DomainError>.Success(true)));
    }

    private void GivenProcessKillFails(string processName, string reason)
    {
        processManager.KillProcessAsync(processName, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                Result<bool, DomainError>.Failure(DomainError.InvalidOperation(reason))));
    }
}
