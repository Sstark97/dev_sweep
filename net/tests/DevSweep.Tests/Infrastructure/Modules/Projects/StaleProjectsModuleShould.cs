using AwesomeAssertions;
using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Common;
using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;
using DevSweep.Infrastructure.Modules.Projects;
using DevSweep.Tests.Builders;
using NSubstitute;

namespace DevSweep.Tests.Infrastructure.Modules.Projects;

internal sealed class StaleProjectsModuleShould
{
    private readonly IFileSystem fileSystem = Substitute.For<IFileSystem>();
    private readonly IEnvironmentProvider environment = Substitute.For<IEnvironmentProvider>();
    private readonly IStaleProjectCleaner nodeProjectCleaner = Substitute.For<IStaleProjectCleaner>();
    private readonly IStaleProjectCleaner mavenProjectCleaner = Substitute.For<IStaleProjectCleaner>();
    private readonly StaleProjectsModule module;

    private static readonly FilePath HomePath = FilePath.Create(Path.Combine("any", "home")).Value;
    private static readonly FilePath NodeModulesDir = FilePath.Create(Path.Combine("any", "home", "projects", "my-app", "node_modules")).Value;
    private static readonly FilePath MavenTargetDir = FilePath.Create(Path.Combine("any", "home", "projects", "my-java-app", "target")).Value;
    private static readonly FilePath NodeProjectDir = FilePath.Create(Path.Combine("any", "home", "projects", "my-app")).Value;
    private static readonly FilePath MavenProjectDir = FilePath.Create(Path.Combine("any", "home", "projects", "my-java-app")).Value;

    public StaleProjectsModuleShould()
    {
        environment.CurrentOperatingSystem.Returns(OperatingSystemType.MacOS);
        environment.HomePath.Returns(HomePath);
        environment.StaleProjectDays.Returns(90);
        environment.StaleProjectMaxDepth.Returns(6);

        nodeProjectCleaner.CleanerName.Returns("node");
        nodeProjectCleaner.ArtifactDirectoryNames.Returns(["node_modules"]);
        nodeProjectCleaner.DirectoriesToSkipDuringScan.Returns(["Library", ".Trash", "node_modules"]);
        nodeProjectCleaner.DirectoriesToExcludeFromActivityCheck.Returns([".git", "node_modules"]);
        nodeProjectCleaner.IsAvailable(Arg.Any<OperatingSystemType>()).Returns(true);
        nodeProjectCleaner.IsProjectDirectory(Arg.Any<IFileSystem>(), Arg.Any<FilePath>()).Returns(true);

        mavenProjectCleaner.CleanerName.Returns("maven");
        mavenProjectCleaner.ArtifactDirectoryNames.Returns(["target"]);
        mavenProjectCleaner.DirectoriesToSkipDuringScan.Returns(["Library", ".Trash", "node_modules", "target"]);
        mavenProjectCleaner.DirectoriesToExcludeFromActivityCheck.Returns([".git", "target"]);
        mavenProjectCleaner.IsAvailable(Arg.Any<OperatingSystemType>()).Returns(true);
        mavenProjectCleaner.IsProjectDirectory(Arg.Any<IFileSystem>(), Arg.Any<FilePath>()).Returns(true);

        GivenNoDirectoriesFound();

        module = new StaleProjectsModule(fileSystem, environment, [nodeProjectCleaner, mavenProjectCleaner]);
    }

    [Test]
    public void BeAvailableOnAllPlatforms()
    {
        module.IsAvailableOnPlatform(OperatingSystemType.MacOS).Should().BeTrue();
        module.IsAvailableOnPlatform(OperatingSystemType.Linux).Should().BeTrue();
        module.IsAvailableOnPlatform(OperatingSystemType.Windows).Should().BeTrue();
    }

    [Test]
    public void NotBeDestructive()
    {
        module.IsDestructive.Should().BeFalse();
    }

    [Test]
    public void HaveProjectsModuleName()
    {
        module.Name.Should().Be(CleanupModuleName.Projects);
    }

    [Test]
    public async Task AggregateItemsFromAllActiveCleaners()
    {
        var nodeSize = new CleanableItemBuilder().Large().Build().Size;
        var mavenSize = new CleanableItemBuilder().Small().Build().Size;
        GivenStaleProject("node_modules", NodeModulesDir, NodeProjectDir, nodeSize);
        GivenStaleProject("target", MavenTargetDir, MavenProjectDir, mavenSize);

        var result = await module.AnalyzeAsync(CancellationToken.None);

        var analysis = result.Value;
        result.IsSuccess.Should().BeTrue();
        analysis.ItemCount().Should().Be(2);
    }

    [Test]
    public async Task SkipCleanersNotAvailableOnCurrentPlatform()
    {
        environment.CurrentOperatingSystem.Returns(OperatingSystemType.Linux);
        mavenProjectCleaner.IsAvailable(OperatingSystemType.Linux).Returns(false);
        var nodeSize = new CleanableItemBuilder().Large().Build().Size;
        GivenStaleProject("node_modules", NodeModulesDir, NodeProjectDir, nodeSize);

        var result = await module.AnalyzeAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await fileSystem.DidNotReceive().FindDirectoriesAsync(
            Arg.Any<FilePath>(), "target", Arg.Any<int>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FindNoItemsWhenNoStaleProjectsExist()
    {
        GivenNoDirectoriesFound();

        var result = await module.AnalyzeAsync(CancellationToken.None);

        var analysis = result.Value;
        result.IsSuccess.Should().BeTrue();
        analysis.ItemCount().Should().Be(0);
    }

    [Test]
    public async Task FindNoItemsWhenProjectsAreRecentlyActive()
    {
        var nodeSize = new CleanableItemBuilder().Large().Build().Size;
        GivenActiveProject("node_modules", NodeModulesDir, NodeProjectDir, nodeSize);

        var result = await module.AnalyzeAsync(CancellationToken.None);

        var analysis = result.Value;
        result.IsSuccess.Should().BeTrue();
        analysis.ItemCount().Should().Be(0);
    }

    [Test]
    public async Task FindStaleItemsWhenProjectsAreInactive()
    {
        var nodeSize = new CleanableItemBuilder().Large().Build().Size;
        GivenStaleProject("node_modules", NodeModulesDir, NodeProjectDir, nodeSize);

        var result = await module.AnalyzeAsync(CancellationToken.None);

        var analysis = result.Value;
        result.IsSuccess.Should().BeTrue();
        analysis.ItemCount().Should().Be(1);
    }

    [Test]
    public async Task SkipNonProjectDirectories()
    {
        nodeProjectCleaner.IsProjectDirectory(Arg.Any<IFileSystem>(), Arg.Any<FilePath>()).Returns(false);
        fileSystem.FindDirectoriesAsync(HomePath, "node_modules", 6,
            Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<FilePath>, DomainError>.Success([NodeModulesDir])));
        fileSystem.DirectoryExists(NodeProjectDir).Returns(true);

        var result = await module.AnalyzeAsync(CancellationToken.None);

        var analysis = result.Value;
        result.IsSuccess.Should().BeTrue();
        analysis.ItemCount().Should().Be(0);
    }

    [Test]
    public async Task DeleteArtifactDirectoriesOnCleanup()
    {
        var nodeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Projects)
            .WithReason("projects:node-stale-120d")
            .WithPath(Path.Combine("any", "home", "projects", "my-app", "node_modules"))
            .Large()
            .Build();
        GivenDeleteSucceeds(nodeItem.Path);

        await module.CleanAsync([nodeItem], CancellationToken.None);

        await fileSystem.Received(1).DeleteDirectoryAsync(nodeItem.Path, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CleanNothingWhenItemListIsEmpty()
    {
        var result = await module.CleanAsync([], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(0);
    }

    [Test]
    public async Task RecordErrorWhenDeleteFails()
    {
        var nodeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Projects)
            .WithReason("projects:node-stale-120d")
            .WithPath(Path.Combine("any", "home", "projects", "my-app", "node_modules"))
            .Large()
            .Build();
        GivenDeleteFails(nodeItem.Path);

        var result = await module.CleanAsync([nodeItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.HasErrors().Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(0);
    }

    [Test]
    public async Task AggregateCleanupResultsFromMultipleItems()
    {
        var nodeItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Projects)
            .WithReason("projects:node-stale-120d")
            .WithPath(Path.Combine("any", "home", "projects", "my-app", "node_modules"))
            .Large()
            .Build();
        var mavenItem = new CleanableItemBuilder()
            .ForModule(CleanupModuleName.Projects)
            .WithReason("projects:maven-stale-95d")
            .WithPath(Path.Combine("any", "home", "projects", "my-java-app", "target"))
            .Small()
            .Build();
        GivenDeleteSucceeds(nodeItem.Path);
        GivenDeleteSucceeds(mavenItem.Path);

        var result = await module.CleanAsync([nodeItem, mavenItem], CancellationToken.None);

        var cleanupResult = result.Value;
        result.IsSuccess.Should().BeTrue();
        cleanupResult.TotalFilesDeleted().Should().Be(2);
    }

    private void GivenNoDirectoriesFound()
    {
        fileSystem.FindDirectoriesAsync(Arg.Any<FilePath>(), Arg.Any<string>(), Arg.Any<int>(),
            Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<FilePath>, DomainError>.Success([])));
    }

    private void GivenStaleProject(
        string artifactDirName,
        FilePath artifactPath, FilePath projectDir, FileSize artifactSize)
    {
        fileSystem.FindDirectoriesAsync(HomePath, artifactDirName, 6,
            Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<FilePath>, DomainError>.Success([artifactPath])));
        fileSystem.DirectoryExists(projectDir).Returns(true);
        fileSystem.Size(artifactPath).Returns(Result<FileSize, DomainError>.Success(artifactSize));
        fileSystem.MostRecentWriteTimeAsync(projectDir,
            Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<DateTime, DomainError>.Success(DateTime.UtcNow.AddDays(-120))));
    }

    private void GivenActiveProject(
        string artifactDirName,
        FilePath artifactPath, FilePath projectDir, FileSize artifactSize)
    {
        fileSystem.FindDirectoriesAsync(HomePath, artifactDirName, 6,
            Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<FilePath>, DomainError>.Success([artifactPath])));
        fileSystem.DirectoryExists(projectDir).Returns(true);
        fileSystem.Size(artifactPath).Returns(Result<FileSize, DomainError>.Success(artifactSize));
        fileSystem.MostRecentWriteTimeAsync(projectDir,
            Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<DateTime, DomainError>.Success(DateTime.UtcNow.AddDays(-10))));
    }

    private void GivenDeleteSucceeds(FilePath path) =>
        fileSystem.DeleteDirectoryAsync(path, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Unit, DomainError>.Success(Unit.Value)));

    private void GivenDeleteFails(FilePath path) =>
        fileSystem.DeleteDirectoryAsync(path, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Unit, DomainError>.Failure(
                DomainError.InvalidOperation("Permission denied"))));
}
