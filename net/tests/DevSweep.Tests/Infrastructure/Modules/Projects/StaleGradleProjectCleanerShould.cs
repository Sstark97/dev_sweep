using AwesomeAssertions;
using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Enums;
using DevSweep.Domain.ValueObjects;
using DevSweep.Infrastructure.Modules.Projects;
using NSubstitute;

namespace DevSweep.Tests.Infrastructure.Modules.Projects;

internal sealed class StaleGradleProjectCleanerShould
{
    private readonly IFileSystem fileSystem = Substitute.For<IFileSystem>();
    private readonly StaleGradleProjectCleaner cleaner = new();

    private static readonly FilePath ProjectDir = FilePath.Create(Path.Combine("any", "projects", "my-app")).Value;
    private static readonly FilePath BuildGradlePath = FilePath.Create(Path.Combine("any", "projects", "my-app", "build.gradle")).Value;
    private static readonly FilePath BuildGradleKtsPath = FilePath.Create(Path.Combine("any", "projects", "my-app", "build.gradle.kts")).Value;
    private static readonly FilePath SettingsGradlePath = FilePath.Create(Path.Combine("any", "projects", "my-app", "settings.gradle")).Value;
    private static readonly FilePath SettingsGradleKtsPath = FilePath.Create(Path.Combine("any", "projects", "my-app", "settings.gradle.kts")).Value;

    [Test]
    public void BeAvailableOnAllPlatforms()
    {
        cleaner.IsAvailable(OperatingSystemType.MacOS).Should().BeTrue();
        cleaner.IsAvailable(OperatingSystemType.Linux).Should().BeTrue();
        cleaner.IsAvailable(OperatingSystemType.Windows).Should().BeTrue();
    }

    [Test]
    public void HaveBuildAsArtifactDirectory()
    {
        cleaner.ArtifactDirectoryNames.Should().ContainSingle()
            .Which.Should().Be("build");
    }

    [Test]
    public void ExcludeGitAndBuildAndGradleFromActivityCheck()
    {
        cleaner.DirectoriesToExcludeFromActivityCheck.Should().Contain(".git");
        cleaner.DirectoriesToExcludeFromActivityCheck.Should().Contain("build");
        cleaner.DirectoriesToExcludeFromActivityCheck.Should().Contain(".gradle");
    }

    [Test]
    public void DetectGradleProjectWhenBuildGradleExists()
    {
        fileSystem.FileExists(BuildGradlePath).Returns(true);

        var isProject = cleaner.IsProjectDirectory(fileSystem, ProjectDir);

        isProject.Should().BeTrue();
    }

    [Test]
    public void DetectGradleProjectWhenSettingsGradleExists()
    {
        fileSystem.FileExists(SettingsGradlePath).Returns(true);

        var isProject = cleaner.IsProjectDirectory(fileSystem, ProjectDir);

        isProject.Should().BeTrue();
    }

    [Test]
    public void DetectGradleProjectWhenKotlinBuildScriptExists()
    {
        fileSystem.FileExists(BuildGradleKtsPath).Returns(true);

        var isProject = cleaner.IsProjectDirectory(fileSystem, ProjectDir);

        isProject.Should().BeTrue();
    }

    [Test]
    public void DetectGradleProjectWhenKotlinSettingsScriptExists()
    {
        fileSystem.FileExists(SettingsGradleKtsPath).Returns(true);

        var isProject = cleaner.IsProjectDirectory(fileSystem, ProjectDir);

        isProject.Should().BeTrue();
    }

    [Test]
    public void NotDetectGradleProjectWhenNoGradleFilesExist()
    {
        fileSystem.FileExists(BuildGradlePath).Returns(false);
        fileSystem.FileExists(BuildGradleKtsPath).Returns(false);
        fileSystem.FileExists(SettingsGradlePath).Returns(false);
        fileSystem.FileExists(SettingsGradleKtsPath).Returns(false);

        var isProject = cleaner.IsProjectDirectory(fileSystem, ProjectDir);

        isProject.Should().BeFalse();
    }
}
