using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Enums;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Infrastructure.Modules.Projects;

public sealed class StalePythonProjectCleaner : IStaleProjectCleaner
{
    public string CleanerName => "python";

    public IReadOnlyList<string> ArtifactDirectoryNames => ["venv", ".venv"];

    public IReadOnlyList<string> DirectoriesToSkipDuringScan =>
        ["Library", ".Trash", "node_modules", "venv", ".venv"];

    public IReadOnlyList<string> DirectoriesToExcludeFromActivityCheck =>
        [".git", "venv", ".venv"];

    public bool IsProjectDirectory(IFileSystem fileSystem, FilePath parentDirectory) => true;

    public bool IsAvailable(OperatingSystemType operatingSystem) => true;
}
