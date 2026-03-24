using DevSweep.Application.Models;
using DevSweep.Domain.Entities;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Infrastructure.Cli.Output.JsonModels;

internal static class JsonMappingExtensions
{
    internal static AnalysisReportJson ToJson(this AnalysisReport report) =>
        new(
            ModuleCount: report.ModuleCount(),
            TotalItems: report.TotalItemCount(),
            TotalSize: report.TotalSize().ToString(),
            TotalSizeBytes: report.Modules().Sum(m => m.TotalSize().InBytes()),
            Modules: [.. from module in report.Modules()
                select new ModuleAnalysisJson(
                    Name: module.Module.ToString(),
                    ItemCount: module.ItemCount(),
                    SafeCount: module.SafeItemCount(),
                    UnsafeCount: module.UnsafeItemCount(),
                    Size: module.TotalSize().ToString(),
                    SizeBytes: module.Items().Sum(item => item.Size.InBytes()))]);

    internal static CleanupCompletionJson ToJson(this IReadOnlyList<CleanupSummary> summaries)
    {
        var totalFiles = summaries.Sum(s => s.Result.TotalFilesDeleted());
        var totalBytes = summaries.Sum(s => s.Result.TotalSpaceFreed().InBytes());
        var totalSize = summaries.Aggregate(
            FileSize.Zero,
            (acc, s) => acc.Add(s.Result.TotalSpaceFreed()));

        return new CleanupCompletionJson(
            TotalFilesDeleted: totalFiles,
            TotalSpaceFreed: totalSize.ToString(),
            TotalSpaceFreedBytes: totalBytes,
            Modules: [.. from summary in summaries
                select new ModuleSummaryJson(
                    Name: summary.Module.ToString(),
                    ItemsScanned: summary.TotalItemsScanned,
                    SafeItems: summary.SafeItemsFound,
                    FilesDeleted: summary.Result.TotalFilesDeleted(),
                    SpaceFreed: summary.Result.TotalSpaceFreed().ToString(),
                    SpaceFreedBytes: summary.Result.TotalSpaceFreed().InBytes(),
                    Errors: summary.Result.ErrorMessages())]);
    }
}
