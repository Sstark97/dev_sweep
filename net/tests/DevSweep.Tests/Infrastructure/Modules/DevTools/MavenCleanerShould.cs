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

internal sealed class MavenCleanerShould
{
    private readonly IFileSystem fileSystem = Substitute.For<IFileSystem>();
    private readonly IEnvironmentProvider environment = Substitute.For<IEnvironmentProvider>();
    private readonly MavenCleaner cleaner;

    private static readonly FilePath RepositoryPath = FilePath.Create(Path.Combine("any", "home", ".m2", "repository")).Value;

    public MavenCleanerShould()
    {
        cleaner = new MavenCleaner(fileSystem, environment);
        environment.MavenRepositoryPath().Returns(RepositoryPath);
    }

    [Test]
    public void BeAvailableOnAllPlatforms()
    {
        cleaner.IsAvailable(OperatingSystemType.MacOS).Should().BeTrue();
        cleaner.IsAvailable(OperatingSystemType.Linux).Should().BeTrue();
        cleaner.IsAvailable(OperatingSystemType.Windows).Should().BeTrue();
    }

    [Test]
    public async Task FindMavenCacheWhenRepositoryDirectoryIsNonEmpty()
    {
        var largeSize = new CleanableItemBuilder().Large().Build().Size;
        GivenRepositoryDirectoryIsNonEmpty(largeSize);

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().Contain(i => i.Reason == "devtools:maven-dependency-cache");
    }

    [Test]
    public async Task FindNoItemsWhenRepositoryDirectoryMissing()
    {
        GivenRepositoryDirectoryMissing();

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().BeEmpty();
    }

    [Test]
    public async Task FindNoItemsWhenRepositoryDirectoryIsEmpty()
    {
        fileSystem.DirectoryExists(RepositoryPath).Returns(true);
        fileSystem.IsDirectoryNotEmpty(RepositoryPath).Returns(false);

        var result = await cleaner.AnalyzeAsync(CancellationToken.None);

        var items = result.Value;
        result.IsSuccess.Should().BeTrue();
        items.Should().BeEmpty();
    }

    [Test]
    public async Task DeleteMavenCacheOnCleanup()
    {
        var mavenItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.DevTools)
            .WithReason("devtools:maven-dependency-cache")
            .WithPath(Path.Combine("any", "home", ".m2", "repository"))
            .Large()
            .Build();
        GivenDeleteSucceeds(mavenItem.Path);

        var result = await cleaner.CleanAsync([mavenItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(1);
        cleanupResult.TotalSpaceFreed().Should().Be(mavenItem.Size);
    }

    [Test]
    public async Task CleanNothingWhenNoMavenItemsProvided()
    {
        var result = await cleaner.CleanAsync([], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(0);
    }

    [Test]
    public async Task RecordErrorWhenDeleteFails()
    {
        var mavenItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.DevTools)
            .WithReason("devtools:maven-dependency-cache")
            .WithPath(Path.Combine("any", "home", ".m2", "repository"))
            .Large()
            .Build();
        GivenDeleteFails(mavenItem.Path);

        var result = await cleaner.CleanAsync([mavenItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.HasErrors().Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(0);
    }

    private void GivenRepositoryDirectoryIsNonEmpty(FileSize size)
    {
        fileSystem.DirectoryExists(RepositoryPath).Returns(true);
        fileSystem.IsDirectoryNotEmpty(RepositoryPath).Returns(true);
        fileSystem.Size(RepositoryPath).Returns(Result<FileSize, DomainError>.Success(size));
    }

    private void GivenRepositoryDirectoryMissing() =>
        fileSystem.DirectoryExists(RepositoryPath).Returns(false);

    private void GivenDeleteSucceeds(FilePath path) =>
        fileSystem.DeleteDirectoryAsync(path, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Unit, DomainError>.Success(Unit.Value)));

    private void GivenDeleteFails(FilePath path) =>
        fileSystem.DeleteDirectoryAsync(path, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Unit, DomainError>.Failure(
                DomainError.InvalidOperation("Permission denied"))));
}
