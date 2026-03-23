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

internal sealed class SystemTempCleanerShould
{
    private readonly IFileSystem fileSystem = Substitute.For<IFileSystem>();
    private readonly IEnvironmentProvider environment = Substitute.For<IEnvironmentProvider>();
    private readonly SystemTempCleaner cleaner;

    private static readonly FilePath TempPath = FilePath.Create(Path.Combine("any", "tmp")).Value;
    private static readonly FilePath StaleTempDir = FilePath.Create(Path.Combine("any", "tmp", "old-session")).Value;
    private static readonly FilePath RecentTempDir = FilePath.Create(Path.Combine("any", "tmp", "recent-session")).Value;

    public SystemTempCleanerShould()
    {
        cleaner = new SystemTempCleaner(fileSystem, environment);
        environment.SystemTempPath().Returns(TempPath);
    }

    [Test]
    public void BeAvailableOnAllPlatforms()
    {
        cleaner.IsAvailable(OperatingSystemType.MacOS).Should().BeTrue();
        cleaner.IsAvailable(OperatingSystemType.Linux).Should().BeTrue();
        cleaner.IsAvailable(OperatingSystemType.Windows).Should().BeTrue();
    }

    [Test]
    public async Task FindStaleTempDirectories()
    {
        var largeSize = new CleanableItemBuilder().Large().Build().Size;
        GivenTempDirectoryExists();
        GivenSubdirectoriesFound([StaleTempDir]);
        GivenSubdirectoryIsStale(StaleTempDir, largeSize);

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().HaveCount(1);
        items.Should().Contain(i => i.Reason == "system:temp-files");
    }

    [Test]
    public async Task SkipRecentTempDirectories()
    {
        GivenTempDirectoryExists();
        GivenSubdirectoriesFound([RecentTempDir]);
        GivenSubdirectoryIsRecent(RecentTempDir);

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().BeEmpty();
    }

    [Test]
    public async Task FindNoItemsWhenTempDirectoryDoesNotExist()
    {
        fileSystem.DirectoryExists(TempPath).Returns(false);

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().BeEmpty();
    }

    [Test]
    public async Task MarkAllTempItemsAsSafe()
    {
        var largeSize = new CleanableItemBuilder().Large().Build().Size;
        GivenTempDirectoryExists();
        GivenSubdirectoriesFound([StaleTempDir]);
        GivenSubdirectoryIsStale(StaleTempDir, largeSize);

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().Contain(i => i.IsSafeToDelete);
    }

    [Test]
    public async Task DeleteMatchingDirectoriesOnCleanup()
    {
        var tempItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.System)
            .WithReason("system:temp-files")
            .WithPath(Path.Combine("any", "tmp", "old-session"))
            .Large()
            .Build();
        GivenDeleteSucceeds(tempItem.Path);

        var result = await cleaner.CleanAsync([tempItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(1);
    }

    [Test]
    public async Task RecordErrorWhenDeleteFails()
    {
        var tempItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.System)
            .WithReason("system:temp-files")
            .WithPath(Path.Combine("any", "tmp", "old-session"))
            .Large()
            .Build();
        GivenDeleteFails(tempItem.Path);

        var result = await cleaner.CleanAsync([tempItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.HasErrors().Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(0);
    }

    [Test]
    public async Task CleanNothingWhenNoTempItemsProvided()
    {
        var result = await cleaner.CleanAsync([], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(0);
    }

    private void GivenTempDirectoryExists()
    {
        fileSystem.DirectoryExists(TempPath).Returns(true);
        fileSystem.IsDirectoryNotEmpty(TempPath).Returns(true);
    }

    private void GivenSubdirectoryIsStale(FilePath path, FileSize size)
    {
        fileSystem.LastWriteTime(path).Returns(DateTime.UtcNow.AddDays(-60));
        fileSystem.Size(path).Returns(Result<FileSize, DomainError>.Success(size));
    }

    private void GivenSubdirectoryIsRecent(FilePath path) =>
        fileSystem.LastWriteTime(path).Returns(DateTime.UtcNow.AddDays(-5));

    private void GivenSubdirectoriesFound(IReadOnlyList<FilePath> paths) =>
        fileSystem.FindDirectoriesAsync(TempPath, "*", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<FilePath>, DomainError>.Success(paths)));

    private void GivenDeleteSucceeds(FilePath path) =>
        fileSystem.DeleteDirectoryAsync(path, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Unit, DomainError>.Success(Unit.Value)));

    private void GivenDeleteFails(FilePath path) =>
        fileSystem.DeleteDirectoryAsync(path, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Unit, DomainError>.Failure(
                DomainError.InvalidOperation("Permission denied"))));
}
