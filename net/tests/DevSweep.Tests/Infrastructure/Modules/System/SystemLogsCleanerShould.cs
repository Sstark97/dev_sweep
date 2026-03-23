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

internal sealed class SystemLogsCleanerShould
{
    private readonly IFileSystem fileSystem = Substitute.For<IFileSystem>();
    private readonly IEnvironmentProvider environment = Substitute.For<IEnvironmentProvider>();
    private readonly SystemLogsCleaner cleaner;

    private static readonly FilePath LogsPath = FilePath.Create(Path.Combine("any", "home", "Library", "Logs")).Value;
    private static readonly FilePath StaleLogDir = FilePath.Create(Path.Combine("any", "home", "Library", "Logs", "some-app")).Value;
    private static readonly FilePath RecentLogDir = FilePath.Create(Path.Combine("any", "home", "Library", "Logs", "recent-app")).Value;
    private static readonly FilePath LinuxLogsPath = FilePath.Create(Path.Combine("any", "var", "log")).Value;
    private static readonly FilePath StaleLinuxLogDir = FilePath.Create(Path.Combine("any", "var", "log", "syslog")).Value;

    public SystemLogsCleanerShould()
    {
        cleaner = new SystemLogsCleaner(fileSystem, environment);
        environment.SystemLogsPath().Returns(LogsPath);
        environment.CurrentOperatingSystem.Returns(OperatingSystemType.MacOS);
    }

    [Test]
    public void BeAvailableOnAllPlatforms()
    {
        cleaner.IsAvailable(OperatingSystemType.MacOS).Should().BeTrue();
        cleaner.IsAvailable(OperatingSystemType.Linux).Should().BeTrue();
        cleaner.IsAvailable(OperatingSystemType.Windows).Should().BeTrue();
    }

    [Test]
    public async Task FindStaleLogDirectories()
    {
        var largeSize = new CleanableItemBuilder().Large().Build().Size;
        GivenLogsDirectoryExists();
        GivenSubdirectoriesFound([StaleLogDir]);
        GivenSubdirectoryIsStale(StaleLogDir, largeSize);

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().HaveCount(1);
        items.Should().Contain(i => i.Reason == "system:user-logs");
    }

    [Test]
    public async Task MarkLinuxSystemLogsAsUnsafe()
    {
        var largeSize = new CleanableItemBuilder().Large().Build().Size;
        environment.CurrentOperatingSystem.Returns(OperatingSystemType.Linux);
        environment.SystemLogsPath().Returns(LinuxLogsPath);
        fileSystem.DirectoryExists(LinuxLogsPath).Returns(true);
        fileSystem.IsDirectoryNotEmpty(LinuxLogsPath).Returns(true);
        GivenSubdirectoriesFoundAt(LinuxLogsPath, [StaleLinuxLogDir]);
        GivenSubdirectoryIsStale(StaleLinuxLogDir, largeSize);

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().Contain(i => i.Reason == "system:system-logs" && !i.IsSafeToDelete);
    }

    [Test]
    public async Task MarkMacOsUserLogsAsSafe()
    {
        var largeSize = new CleanableItemBuilder().Large().Build().Size;
        environment.CurrentOperatingSystem.Returns(OperatingSystemType.MacOS);
        GivenLogsDirectoryExists();
        GivenSubdirectoriesFound([StaleLogDir]);
        GivenSubdirectoryIsStale(StaleLogDir, largeSize);

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().Contain(i => i.Reason == "system:user-logs" && i.IsSafeToDelete);
    }

    [Test]
    public async Task SkipRecentLogDirectories()
    {
        GivenLogsDirectoryExists();
        GivenSubdirectoriesFound([RecentLogDir]);
        GivenSubdirectoryIsRecent(RecentLogDir);

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().BeEmpty();
    }

    [Test]
    public async Task FindNoItemsWhenLogsDirectoryDoesNotExist()
    {
        fileSystem.DirectoryExists(LogsPath).Returns(false);

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().BeEmpty();
    }

    [Test]
    public async Task DeleteMatchingDirectoriesOnCleanup()
    {
        var logItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.System)
            .WithReason("system:user-logs")
            .WithPath(Path.Combine("any", "home", "Library", "Logs", "some-app"))
            .Large()
            .Build();
        GivenDeleteSucceeds(logItem.Path);

        var result = await cleaner.CleanAsync([logItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(1);
    }

    [Test]
    public async Task RecordErrorWhenDeleteFails()
    {
        var logItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.System)
            .WithReason("system:user-logs")
            .WithPath(Path.Combine("any", "home", "Library", "Logs", "some-app"))
            .Large()
            .Build();
        GivenDeleteFails(logItem.Path);

        var result = await cleaner.CleanAsync([logItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.HasErrors().Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(0);
    }

    private void GivenLogsDirectoryExists()
    {
        fileSystem.DirectoryExists(LogsPath).Returns(true);
        fileSystem.IsDirectoryNotEmpty(LogsPath).Returns(true);
    }

    private void GivenSubdirectoryIsStale(FilePath path, FileSize size)
    {
        fileSystem.LastWriteTime(path).Returns(DateTime.UtcNow.AddDays(-60));
        fileSystem.Size(path).Returns(Result<FileSize, DomainError>.Success(size));
    }

    private void GivenSubdirectoryIsRecent(FilePath path) =>
        fileSystem.LastWriteTime(path).Returns(DateTime.UtcNow.AddDays(-5));

    private void GivenSubdirectoriesFound(IReadOnlyList<FilePath> paths) =>
        fileSystem.FindDirectoriesAsync(LogsPath, "*", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<FilePath>, DomainError>.Success(paths)));

    private void GivenSubdirectoriesFoundAt(FilePath basePath, IReadOnlyList<FilePath> paths) =>
        fileSystem.FindDirectoriesAsync(basePath, "*", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<FilePath>, DomainError>.Success(paths)));

    private void GivenDeleteSucceeds(FilePath path) =>
        fileSystem.DeleteDirectoryAsync(path, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Unit, DomainError>.Success(Unit.Value)));

    private void GivenDeleteFails(FilePath path) =>
        fileSystem.DeleteDirectoryAsync(path, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Unit, DomainError>.Failure(
                DomainError.InvalidOperation("Permission denied"))));
}
