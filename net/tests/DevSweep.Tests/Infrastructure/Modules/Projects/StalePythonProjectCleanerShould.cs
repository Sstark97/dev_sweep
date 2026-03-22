using AwesomeAssertions;
using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Enums;
using DevSweep.Domain.ValueObjects;
using DevSweep.Infrastructure.Modules.Projects;
using NSubstitute;

namespace DevSweep.Tests.Infrastructure.Modules.Projects;

internal sealed class StalePythonProjectCleanerShould
{
    private readonly IFileSystem fileSystem = Substitute.For<IFileSystem>();
    private readonly StalePythonProjectCleaner cleaner = new();

    private static readonly FilePath ProjectDir = FilePath.Create(Path.Combine("any", "projects", "my-app")).Value;

    [Test]
    public void BeAvailableOnAllPlatforms()
    {
        cleaner.IsAvailable(OperatingSystemType.MacOS).Should().BeTrue();
        cleaner.IsAvailable(OperatingSystemType.Linux).Should().BeTrue();
        cleaner.IsAvailable(OperatingSystemType.Windows).Should().BeTrue();
    }

    [Test]
    public void HaveVenvDirectoriesAsArtifacts()
    {
        cleaner.ArtifactDirectoryNames.Should().Contain("venv");
        cleaner.ArtifactDirectoryNames.Should().Contain(".venv");
    }

    [Test]
    public void ExcludeGitAndVenvFromActivityCheck()
    {
        cleaner.DirectoriesToExcludeFromActivityCheck.Should().Contain(".git");
        cleaner.DirectoriesToExcludeFromActivityCheck.Should().Contain("venv");
        cleaner.DirectoriesToExcludeFromActivityCheck.Should().Contain(".venv");
    }

    [Test]
    public void TreatAnyDirectoryAsPythonProject()
    {
        var isProject = cleaner.IsProjectDirectory(fileSystem, ProjectDir);

        isProject.Should().BeTrue();
    }
}
