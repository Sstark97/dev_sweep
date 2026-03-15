using AwesomeAssertions;
using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Common;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;
using DevSweep.Infrastructure.Modules.DevTools;
using DevSweep.Tests.Builders;
using NSubstitute;

namespace DevSweep.Tests.Infrastructure.Modules.DevTools;

internal sealed class PythonCleanerShould
{
    private readonly IFileSystem fileSystem = Substitute.For<IFileSystem>();
    private readonly IEnvironmentProvider environment = Substitute.For<IEnvironmentProvider>();
    private readonly PythonCleaner cleaner;

    private static readonly FilePath PipCachePath = FilePath.Create(Path.Combine("any", "home", "Library", "Caches", "pip")).Value;
    private static readonly FilePath PoetryCachePath = FilePath.Create(Path.Combine("any", "home", "Library", "Caches", "pypoetry")).Value;

    public PythonCleanerShould()
    {
        cleaner = new PythonCleaner(fileSystem, environment);
        environment.PythonCachePath().Returns(PipCachePath);
        environment.PoetryCachePath().Returns(PoetryCachePath);
    }

    [Test]
    public void BeAvailableOnAllPlatforms()
    {
        cleaner.IsAvailable(OperatingSystemType.MacOS).Should().BeTrue();
        cleaner.IsAvailable(OperatingSystemType.Linux).Should().BeTrue();
        cleaner.IsAvailable(OperatingSystemType.Windows).Should().BeTrue();
    }

    [Test]
    public async Task FindPipCacheWhenDirectoryIsNonEmpty()
    {
        var largeSize = new CleanableItemBuilder().Large().Build().Size;
        GivenDirectoryIsNonEmpty(PipCachePath, largeSize);
        GivenDirectoryMissing(PoetryCachePath);

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().Contain(i => i.Reason == "devtools:pip-cache");
    }

    [Test]
    public async Task FindPoetryCacheWhenDirectoryIsNonEmpty()
    {
        var largeSize = new CleanableItemBuilder().Large().Build().Size;
        GivenDirectoryMissing(PipCachePath);
        GivenDirectoryIsNonEmpty(PoetryCachePath, largeSize);

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().Contain(i => i.Reason == "devtools:poetry-cache");
    }

    [Test]
    public async Task FindBothCachesWhenBothExist()
    {
        var smallSize = new CleanableItemBuilder().Small().Build().Size;
        var largeSize = new CleanableItemBuilder().Large().Build().Size;
        GivenDirectoryIsNonEmpty(PipCachePath, smallSize);
        GivenDirectoryIsNonEmpty(PoetryCachePath, largeSize);

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().HaveCount(2);
    }

    [Test]
    public async Task FindNoItemsWhenNoPythonDirectoriesExist()
    {
        GivenDirectoryMissing(PipCachePath);
        GivenDirectoryMissing(PoetryCachePath);

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().BeEmpty();
    }

    [Test]
    public async Task DeletePipCacheOnCleanup()
    {
        var pipItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.DevTools)
            .WithReason("devtools:pip-cache")
            .WithPath(Path.Combine("any", "home", "Library", "Caches", "pip"))
            .Large()
            .Build();
        GivenDeleteSucceeds(pipItem.Path);

        var result = await cleaner.CleanAsync([pipItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(1);
        cleanupResult.TotalSpaceFreed().Should().Be(pipItem.Size);
    }

    [Test]
    public async Task DeletePoetryCacheOnCleanup()
    {
        var poetryItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.DevTools)
            .WithReason("devtools:poetry-cache")
            .WithPath(Path.Combine("any", "home", "Library", "Caches", "pypoetry"))
            .Large()
            .Build();
        GivenDeleteSucceeds(poetryItem.Path);

        var result = await cleaner.CleanAsync([poetryItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(1);
        cleanupResult.TotalSpaceFreed().Should().Be(poetryItem.Size);
    }

    [Test]
    public async Task RecordErrorWhenDeleteFails()
    {
        var pipItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.DevTools)
            .WithReason("devtools:pip-cache")
            .WithPath(Path.Combine("any", "home", "Library", "Caches", "pip"))
            .Large()
            .Build();
        GivenDeleteFails(pipItem.Path);

        var result = await cleaner.CleanAsync([pipItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.HasErrors().Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(0);
    }

    private void GivenDirectoryIsNonEmpty(FilePath path, FileSize size)
    {
        fileSystem.DirectoryExists(path).Returns(true);
        fileSystem.IsDirectoryNotEmpty(path).Returns(true);
        fileSystem.Size(path).Returns(Result<FileSize, DomainError>.Success(size));
    }

    private void GivenDirectoryMissing(FilePath path) =>
        fileSystem.DirectoryExists(path).Returns(false);

    private void GivenDeleteSucceeds(FilePath path) =>
        fileSystem.DeleteDirectoryAsync(path, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Unit, DomainError>.Success(Unit.Value)));

    private void GivenDeleteFails(FilePath path) =>
        fileSystem.DeleteDirectoryAsync(path, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Unit, DomainError>.Failure(
                DomainError.InvalidOperation("Permission denied"))));
}
