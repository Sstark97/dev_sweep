using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Enums;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Infrastructure.Modules.Projects;

public sealed class StaleGradleProjectCleaner : IStaleProjectCleaner
{
    private static readonly string[] GradleSentinelFiles =
        ["build.gradle", "build.gradle.kts", "settings.gradle", "settings.gradle.kts"];

    public string CleanerName => "gradle";

    public IReadOnlyList<string> ArtifactDirectoryNames => ["build"];

    public IReadOnlyList<string> DirectoriesToSkipDuringScan =>
        ["Library", ".Trash", "node_modules", "build"];

    public IReadOnlyList<string> DirectoriesToExcludeFromActivityCheck =>
        [".git", "build", ".gradle"];

    public bool IsProjectDirectory(IFileSystem fileSystem, FilePath parentDirectory) =>
        (from sentinelFile in GradleSentinelFiles
         let filePath = FilePath.Create(Path.Combine(parentDirectory.ToString(), sentinelFile))
         where filePath.IsSuccess && fileSystem.FileExists(filePath.Value)
         select sentinelFile).Any();

    public bool IsAvailable(OperatingSystemType operatingSystem) => true;
}
