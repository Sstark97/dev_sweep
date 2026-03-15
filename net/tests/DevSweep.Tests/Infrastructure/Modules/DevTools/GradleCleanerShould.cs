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

internal sealed class GradleCleanerShould
{
    private readonly IFileSystem fileSystem = Substitute.For<IFileSystem>();
    private readonly IEnvironmentProvider environment = Substitute.For<IEnvironmentProvider>();
    private readonly GradleCleaner cleaner;

    private static readonly FilePath CachePath = FilePath.Create(Path.Combine("any", "home", ".gradle", "caches")).Value;
    private static readonly FilePath WrapperPath = FilePath.Create(Path.Combine("any", "home", ".gradle", "wrapper")).Value;

    public GradleCleanerShould()
    {
        cleaner = new GradleCleaner(fileSystem, environment);
        environment.GradleCachePath().Returns(CachePath);
        environment.GradleWrapperPath().Returns(WrapperPath);
    }

    [Test]
    public void BeAvailableOnAllPlatforms()
    {
        cleaner.IsAvailable(OperatingSystemType.MacOS).Should().BeTrue();
        cleaner.IsAvailable(OperatingSystemType.Linux).Should().BeTrue();
        cleaner.IsAvailable(OperatingSystemType.Windows).Should().BeTrue();
    }

    [Test]
    public async Task FindGradleCacheWhenCacheDirectoryIsNonEmpty()
    {
        var largeSize = new CleanableItemBuilder().Large().Build().Size;
        GivenCacheDirectoryIsNonEmpty(largeSize);
        GivenWrapperDirectoryMissing();

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().Contain(i => i.Reason == "devtools:gradle-cache");
    }

    [Test]
    public async Task FindGradleWrapperAsUnsafeItem()
    {
        var largeSize = new CleanableItemBuilder().Large().Build().Size;
        GivenCacheDirectoryMissing();
        GivenWrapperDirectoryIsNonEmpty(largeSize);

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().Contain(i => i.Reason == "devtools:gradle-wrapper-nuclear" && !i.IsSafeToDelete);
    }

    [Test]
    public async Task FindNoItemsWhenGradleDirectoriesMissing()
    {
        GivenCacheDirectoryMissing();
        GivenWrapperDirectoryMissing();

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().BeEmpty();
    }

    [Test]
    public async Task IncludeBothCacheAndWrapperWhenBothExist()
    {
        var smallSize = new CleanableItemBuilder().Small().Build().Size;
        var largeSize = new CleanableItemBuilder().Large().Build().Size;
        GivenCacheDirectoryIsNonEmpty(smallSize);
        GivenWrapperDirectoryIsNonEmpty(largeSize);

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().HaveCount(2);
    }

    [Test]
    public async Task DeleteCacheDirectoryOnCleanup()
    {
        var cacheItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.DevTools)
            .WithReason("devtools:gradle-cache")
            .WithPath(Path.Combine("any", "home", ".gradle", "caches"))
            .Large()
            .Build();
        GivenDeleteSucceeds(cacheItem.Path);

        var result = await cleaner.CleanAsync([cacheItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(1);
    }

    [Test]
    public async Task DeleteWrapperDirectoryWhenUnsafeItemProvided()
    {
        var wrapperItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.DevTools)
            .Unsafe()
            .WithReason("devtools:gradle-wrapper-nuclear")
            .WithPath(Path.Combine("any", "home", ".gradle", "wrapper"))
            .Large()
            .Build();
        GivenDeleteSucceeds(wrapperItem.Path);

        var result = await cleaner.CleanAsync([wrapperItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(1);
    }

    [Test]
    public async Task RecordErrorWhenDeleteFails()
    {
        var cacheItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.DevTools)
            .WithReason("devtools:gradle-cache")
            .WithPath(Path.Combine("any", "home", ".gradle", "caches"))
            .Large()
            .Build();
        GivenDeleteFails(cacheItem.Path);

        var result = await cleaner.CleanAsync([cacheItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.HasErrors().Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(0);
    }

    private void GivenCacheDirectoryIsNonEmpty(FileSize size)
    {
        fileSystem.DirectoryExists(CachePath).Returns(true);
        fileSystem.IsDirectoryNotEmpty(CachePath).Returns(true);
        fileSystem.Size(CachePath).Returns(Result<FileSize, DomainError>.Success(size));
    }

    private void GivenCacheDirectoryMissing() =>
        fileSystem.DirectoryExists(CachePath).Returns(false);

    private void GivenWrapperDirectoryIsNonEmpty(FileSize size)
    {
        fileSystem.DirectoryExists(WrapperPath).Returns(true);
        fileSystem.IsDirectoryNotEmpty(WrapperPath).Returns(true);
        fileSystem.Size(WrapperPath).Returns(Result<FileSize, DomainError>.Success(size));
    }

    private void GivenWrapperDirectoryMissing() =>
        fileSystem.DirectoryExists(WrapperPath).Returns(false);

    private void GivenDeleteSucceeds(FilePath path) =>
        fileSystem.DeleteDirectoryAsync(path, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Unit, DomainError>.Success(Unit.Value)));

    private void GivenDeleteFails(FilePath path) =>
        fileSystem.DeleteDirectoryAsync(path, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Unit, DomainError>.Failure(
                DomainError.InvalidOperation("Permission denied"))));
}
