using AwesomeAssertions;
using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Common;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;
using DevSweep.Infrastructure.Modules.System;
using DevSweep.Tests.Builders;
using NSubstitute;

namespace DevSweep.Tests.Infrastructure.Modules.System;

internal sealed class SystemCacheCleanerShould
{
    private readonly IFileSystem fileSystem = Substitute.For<IFileSystem>();
    private readonly IEnvironmentProvider environment = Substitute.For<IEnvironmentProvider>();
    private readonly SystemCacheCleaner cleaner;

    private static readonly FilePath CachePath = FilePath.Create(Path.Combine("any", "home", "Library", "Caches")).Value;
    private static readonly FilePath StaleSubdirectory = FilePath.Create(Path.Combine("any", "home", "Library", "Caches", "some-app")).Value;
    private static readonly FilePath BrowserCacheDir = FilePath.Create(Path.Combine("any", "home", "Library", "Caches", "Google")).Value;
    private static readonly FilePath RecentSubdirectory = FilePath.Create(Path.Combine("any", "home", "Library", "Caches", "recent-app")).Value;

    public SystemCacheCleanerShould()
    {
        cleaner = new SystemCacheCleaner(fileSystem, environment);
        environment.SystemCachePath().Returns(CachePath);
    }

    [Test]
    public void BeAvailableOnAllPlatforms()
    {
        cleaner.IsAvailable(OperatingSystemType.MacOS).Should().BeTrue();
        cleaner.IsAvailable(OperatingSystemType.Linux).Should().BeTrue();
        cleaner.IsAvailable(OperatingSystemType.Windows).Should().BeTrue();
    }

    [Test]
    public async Task FindStaleCacheDirectoriesOlderThanThreshold()
    {
        var largeSize = new CleanableItemBuilder().Large().Build().Size;
        GivenCacheDirectoryExists();
        GivenSubdirectoriesFound([StaleSubdirectory]);
        GivenSubdirectoryIsStale(StaleSubdirectory, largeSize);

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().Contain(i => i.Reason == "system:user-cache");
        items.Should().HaveCount(1);
    }

    [Test]
    public async Task SkipWhitelistedDirectories()
    {
        GivenCacheDirectoryExists();
        GivenSubdirectoriesFound([BrowserCacheDir]);
        GivenSubdirectoryIsStale(BrowserCacheDir, new CleanableItemBuilder().Large().Build().Size);

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().BeEmpty();
    }

    [Test]
    public async Task SkipRecentlyModifiedDirectories()
    {
        GivenCacheDirectoryExists();
        GivenSubdirectoriesFound([RecentSubdirectory]);
        GivenSubdirectoryIsRecent(RecentSubdirectory);

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().BeEmpty();
    }

    [Test]
    public async Task FindNoItemsWhenCacheDirectoryDoesNotExist()
    {
        fileSystem.DirectoryExists(CachePath).Returns(false);

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().BeEmpty();
    }

    [Test]
    public async Task FindNoItemsWhenCacheDirectoryIsEmpty()
    {
        fileSystem.DirectoryExists(CachePath).Returns(true);
        fileSystem.IsDirectoryNotEmpty(CachePath).Returns(false);

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().BeEmpty();
    }

    [Test]
    public async Task DeleteMatchingDirectoriesOnCleanup()
    {
        var cacheItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.System)
            .WithReason("system:user-cache")
            .WithPath(Path.Combine("any", "home", "Library", "Caches", "some-app"))
            .Large()
            .Build();
        GivenDeleteSucceeds(cacheItem.Path);

        var result = await cleaner.CleanAsync([cacheItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(1);
    }

    [Test]
    public async Task RecordErrorWhenDeleteFails()
    {
        var cacheItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.System)
            .WithReason("system:user-cache")
            .WithPath(Path.Combine("any", "home", "Library", "Caches", "some-app"))
            .Large()
            .Build();
        GivenDeleteFails(cacheItem.Path);

        var result = await cleaner.CleanAsync([cacheItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.HasErrors().Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(0);
    }

    [Test]
    public async Task CleanNothingWhenNoSystemCacheItemsProvided()
    {
        var result = await cleaner.CleanAsync([], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(0);
    }

    private void GivenCacheDirectoryExists()
    {
        fileSystem.DirectoryExists(CachePath).Returns(true);
        fileSystem.IsDirectoryNotEmpty(CachePath).Returns(true);
    }

    private void GivenSubdirectoryIsStale(FilePath path, FileSize size)
    {
        fileSystem.LastWriteTime(path).Returns(DateTime.UtcNow.AddDays(-60));
        fileSystem.Size(path).Returns(Result<FileSize, DomainError>.Success(size));
    }

    private void GivenSubdirectoryIsRecent(FilePath path) =>
        fileSystem.LastWriteTime(path).Returns(DateTime.UtcNow.AddDays(-5));

    private void GivenSubdirectoriesFound(IReadOnlyList<FilePath> paths) =>
        fileSystem.FindDirectoriesAsync(CachePath, "*", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<FilePath>, DomainError>.Success(paths)));

    private void GivenDeleteSucceeds(FilePath path) =>
        fileSystem.DeleteDirectoryAsync(path, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Unit, DomainError>.Success(Unit.Value)));

    private void GivenDeleteFails(FilePath path) =>
        fileSystem.DeleteDirectoryAsync(path, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Unit, DomainError>.Failure(
                DomainError.InvalidOperation("Permission denied"))));
}
