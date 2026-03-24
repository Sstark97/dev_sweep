namespace DevSweep.Infrastructure.Cli.Output.JsonModels;

internal sealed record AnalysisReportJson(
    int ModuleCount,
    int TotalItems,
    string TotalSize,
    long TotalSizeBytes,
    IReadOnlyList<ModuleAnalysisJson> Modules);

internal sealed record ModuleAnalysisJson(
    string Name,
    int ItemCount,
    int SafeCount,
    int UnsafeCount,
    string Size,
    long SizeBytes);
