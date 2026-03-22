using AwesomeAssertions;
using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Enums;
using DevSweep.Domain.ValueObjects;
using DevSweep.Infrastructure.Modules.Projects;
using NSubstitute;

namespace DevSweep.Tests.Infrastructure.Modules.Projects;

internal sealed class StaleNodeProjectCleanerShould
{
    private readonly IFileSystem fileSystem = Substitute.For<IFileSystem>();
    private readonly StaleNodeProjectCleaner cleaner = new();

    private static readonly FilePath ProjectDir = FilePath.Create(Path.Combine("any", "projects", "my-app")).Value;

    [Test]
    public void BeAvailableOnAllPlatforms()
    {
        cleaner.IsAvailable(OperatingSystemType.MacOS).Should().BeTrue();
        cleaner.IsAvailable(OperatingSystemType.Linux).Should().BeTrue();
        cleaner.IsAvailable(OperatingSystemType.Windows).Should().BeTrue();
    }

    [Test]
    public void HaveNodeModulesAsArtifactDirectory()
    {
        cleaner.ArtifactDirectoryNames.Should().ContainSingle()
            .Which.Should().Be("node_modules");
    }

    [Test]
    public void ExcludeGitAndNodeModulesFromActivityCheck()
    {
        cleaner.DirectoriesToExcludeFromActivityCheck.Should().Contain(".git");
        cleaner.DirectoriesToExcludeFromActivityCheck.Should().Contain("node_modules");
    }

    [Test]
    public void TreatAnyDirectoryAsNodeProject()
    {
        var isProject = cleaner.IsProjectDirectory(fileSystem, ProjectDir);

        isProject.Should().BeTrue();
    }
}
