using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Enums;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Infrastructure.Modules.Projects;

public sealed class StaleMavenProjectCleaner : IStaleProjectCleaner
{
    public string CleanerName => "maven";

    public IReadOnlyList<string> ArtifactDirectoryNames => ["target"];

    public IReadOnlyList<string> DirectoriesToSkipDuringScan =>
        ["Library", ".Trash", "node_modules", "target"];

    public IReadOnlyList<string> DirectoriesToExcludeFromActivityCheck =>
        [".git", "target"];

    public bool IsProjectDirectory(IFileSystem fileSystem, FilePath parentDirectory)
    {
        var pomPath = FilePath.Create(Path.Combine(parentDirectory.ToString(), "pom.xml"));
        return pomPath.IsSuccess && fileSystem.FileExists(pomPath.Value);
    }

    public bool IsAvailable(OperatingSystemType operatingSystem) => true;
}
