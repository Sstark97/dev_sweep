using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Enums;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Infrastructure.Modules.Projects;

public sealed class StaleNodeProjectCleaner : IStaleProjectCleaner
{
    public string CleanerName => "node";

    public IReadOnlyList<string> ArtifactDirectoryNames => ["node_modules"];

    public IReadOnlyList<string> DirectoriesToSkipDuringScan =>
        ["Library", ".Trash", "node_modules"];

    public IReadOnlyList<string> DirectoriesToExcludeFromActivityCheck =>
        [".git", "node_modules"];

    public bool IsProjectDirectory(IFileSystem fileSystem, FilePath parentDirectory) => true;

    public bool IsAvailable(OperatingSystemType operatingSystem) => true;
}
