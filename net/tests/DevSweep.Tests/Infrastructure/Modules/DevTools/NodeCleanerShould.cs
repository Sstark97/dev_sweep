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

internal sealed class NodeCleanerShould
{
    private readonly IFileSystem fileSystem = Substitute.For<IFileSystem>();
    private readonly IEnvironmentProvider environment = Substitute.For<IEnvironmentProvider>();
    private readonly NodeCleaner cleaner;

    private static readonly FilePath NpmCachePath = FilePath.Create(Path.Combine("any", "home", ".npm", "_cacache")).Value;
    private static readonly FilePath YarnCachePath = FilePath.Create(Path.Combine("any", "home", "Library", "Caches", "Yarn")).Value;
    private static readonly FilePath PnpmStorePath = FilePath.Create(Path.Combine("any", "home", "Library", "pnpm", "store")).Value;
    private static readonly FilePath NvmCachePath = FilePath.Create(Path.Combine("any", "home", ".nvm", ".cache")).Value;
    private static readonly FilePath NpmFullPath = FilePath.Create(Path.Combine("any", "home", ".npm")).Value;

    public NodeCleanerShould()
    {
        cleaner = new NodeCleaner(fileSystem, environment);
        environment.NodeModulesGlobalPath().Returns(NpmCachePath);
        environment.YarnCachePath().Returns(YarnCachePath);
        environment.PnpmStorePath().Returns(PnpmStorePath);
        environment.NvmCachePath().Returns(NvmCachePath);
        environment.NpmFullPath().Returns(NpmFullPath);
    }

    [Test]
    public void BeAvailableOnAllPlatforms()
    {
        cleaner.IsAvailable(OperatingSystemType.MacOS).Should().BeTrue();
        cleaner.IsAvailable(OperatingSystemType.Linux).Should().BeTrue();
        cleaner.IsAvailable(OperatingSystemType.Windows).Should().BeTrue();
    }

    [Test]
    public async Task FindNpmCacheWhenDirectoryIsNonEmpty()
    {
        var largeSize = new CleanableItemBuilder().Large().Build().Size;
        GivenDirectoryIsNonEmpty(NpmCachePath, largeSize);
        GivenAllOtherDirectoriesMissing(NpmCachePath);

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().Contain(i => i.Reason == "devtools:npm-cache");
    }

    [Test]
    public async Task FindYarnCacheWhenDirectoryIsNonEmpty()
    {
        var largeSize = new CleanableItemBuilder().Large().Build().Size;
        GivenDirectoryIsNonEmpty(YarnCachePath, largeSize);
        GivenAllOtherDirectoriesMissing(YarnCachePath);

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().Contain(i => i.Reason == "devtools:yarn-cache");
    }

    [Test]
    public async Task FindPnpmStoreWhenDirectoryIsNonEmpty()
    {
        var largeSize = new CleanableItemBuilder().Large().Build().Size;
        GivenDirectoryIsNonEmpty(PnpmStorePath, largeSize);
        GivenAllOtherDirectoriesMissing(PnpmStorePath);

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().Contain(i => i.Reason == "devtools:pnpm-store");
    }

    [Test]
    public async Task FindNvmCacheWhenDirectoryIsNonEmpty()
    {
        var largeSize = new CleanableItemBuilder().Large().Build().Size;
        GivenDirectoryIsNonEmpty(NvmCachePath, largeSize);
        GivenAllOtherDirectoriesMissing(NvmCachePath);

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().Contain(i => i.Reason == "devtools:nvm-cache");
    }

    [Test]
    public async Task IncludeNpmDirectoryAsUnsafeNuclearItem()
    {
        var largeSize = new CleanableItemBuilder().Large().Build().Size;
        GivenDirectoryIsNonEmpty(NpmFullPath, largeSize);
        GivenAllOtherDirectoriesMissing(NpmFullPath);

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().Contain(i => i.Reason == "devtools:npm-directory-nuclear" && !i.IsSafeToDelete);
    }

    [Test]
    public async Task FindNoItemsWhenNoNodeDirectoriesExist()
    {
        GivenAllDirectoriesMissing();

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().BeEmpty();
    }

    [Test]
    public async Task DeleteAllMatchingDirectoriesOnCleanup()
    {
        var npmItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.DevTools)
            .WithReason("devtools:npm-cache")
            .WithPath(Path.Combine("any", "home", ".npm", "_cacache"))
            .Small()
            .Build();
        var yarnItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.DevTools)
            .WithReason("devtools:yarn-cache")
            .WithPath(Path.Combine("any", "home", "Library", "Caches", "Yarn"))
            .Large()
            .Build();
        GivenDeleteSucceeds(npmItem.Path);
        GivenDeleteSucceeds(yarnItem.Path);

        var result = await cleaner.CleanAsync([npmItem, yarnItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(2);
    }

    [Test]
    public async Task CleanNothingWhenNoNodeItemsProvided()
    {
        var result = await cleaner.CleanAsync([], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(0);
    }

    [Test]
    public async Task RecordErrorWhenDeleteFails()
    {
        var npmItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.DevTools)
            .WithReason("devtools:npm-cache")
            .WithPath(Path.Combine("any", "home", ".npm", "_cacache"))
            .Large()
            .Build();
        GivenDeleteFails(npmItem.Path);

        var result = await cleaner.CleanAsync([npmItem], CancellationToken.None);

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

    private void GivenAllOtherDirectoriesMissing(FilePath except)
    {
        foreach (var path in new[] { NpmCachePath, YarnCachePath, PnpmStorePath, NvmCachePath, NpmFullPath })
        {
            if (path != except)
                fileSystem.DirectoryExists(path).Returns(false);
        }
    }

    private void GivenAllDirectoriesMissing()
    {
        foreach (var path in new[] { NpmCachePath, YarnCachePath, PnpmStorePath, NvmCachePath, NpmFullPath })
            fileSystem.DirectoryExists(path).Returns(false);
    }

    private void GivenDeleteSucceeds(FilePath path) =>
        fileSystem.DeleteDirectoryAsync(path, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Unit, DomainError>.Success(Unit.Value)));

    private void GivenDeleteFails(FilePath path) =>
        fileSystem.DeleteDirectoryAsync(path, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Unit, DomainError>.Failure(
                DomainError.InvalidOperation("Permission denied"))));
}
