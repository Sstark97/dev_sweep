namespace DevSweep.Infrastructure.Cli.Output.JsonModels;

internal sealed record CleanupCompletionJson(
    int TotalFilesDeleted,
    string TotalSpaceFreed,
    long TotalSpaceFreedBytes,
    IReadOnlyList<ModuleSummaryJson> Modules);

internal sealed record ModuleSummaryJson(
    string Name,
    int ItemsScanned,
    int SafeItems,
    int FilesDeleted,
    string SpaceFreed,
    long SpaceFreedBytes,
    IReadOnlyList<string> Errors);
