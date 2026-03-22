using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Enums;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Infrastructure.Modules.Projects;

public interface IStaleProjectCleaner
{
    string CleanerName { get; }
    IReadOnlyList<string> ArtifactDirectoryNames { get; }
    IReadOnlyList<string> DirectoriesToSkipDuringScan { get; }
    IReadOnlyList<string> DirectoriesToExcludeFromActivityCheck { get; }
    bool IsProjectDirectory(IFileSystem fileSystem, FilePath parentDirectory);
    bool IsAvailable(OperatingSystemType operatingSystem);
}
