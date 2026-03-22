using AwesomeAssertions;
using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Enums;
using DevSweep.Domain.ValueObjects;
using DevSweep.Infrastructure.Modules.Projects;
using NSubstitute;

namespace DevSweep.Tests.Infrastructure.Modules.Projects;

internal sealed class StaleMavenProjectCleanerShould
{
    private readonly IFileSystem fileSystem = Substitute.For<IFileSystem>();
    private readonly StaleMavenProjectCleaner cleaner = new();

    private static readonly FilePath ProjectDir = FilePath.Create(Path.Combine("any", "projects", "my-app")).Value;
    private static readonly FilePath PomXmlPath = FilePath.Create(Path.Combine("any", "projects", "my-app", "pom.xml")).Value;

    [Test]
    public void BeAvailableOnAllPlatforms()
    {
        cleaner.IsAvailable(OperatingSystemType.MacOS).Should().BeTrue();
        cleaner.IsAvailable(OperatingSystemType.Linux).Should().BeTrue();
        cleaner.IsAvailable(OperatingSystemType.Windows).Should().BeTrue();
    }

    [Test]
    public void HaveTargetAsArtifactDirectory()
    {
        cleaner.ArtifactDirectoryNames.Should().ContainSingle()
            .Which.Should().Be("target");
    }

    [Test]
    public void ExcludeGitAndTargetFromActivityCheck()
    {
        cleaner.DirectoriesToExcludeFromActivityCheck.Should().Contain(".git");
        cleaner.DirectoriesToExcludeFromActivityCheck.Should().Contain("target");
    }

    [Test]
    public void DetectMavenProjectWhenPomXmlExists()
    {
        fileSystem.FileExists(PomXmlPath).Returns(true);

        var isProject = cleaner.IsProjectDirectory(fileSystem, ProjectDir);

        isProject.Should().BeTrue();
    }

    [Test]
    public void NotDetectMavenProjectWhenPomXmlMissing()
    {
        fileSystem.FileExists(PomXmlPath).Returns(false);

        var isProject = cleaner.IsProjectDirectory(fileSystem, ProjectDir);

        isProject.Should().BeFalse();
    }
}
