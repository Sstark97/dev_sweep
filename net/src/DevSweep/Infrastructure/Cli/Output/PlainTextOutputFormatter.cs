using DevSweep.Application.Models;
using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Entities;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Infrastructure.Cli.Output;

public sealed class PlainTextOutputFormatter(TextWriter writer) : IOutputFormatter
{
    public void Info(string message) => writer.WriteLine($"[INFO] {message}");

    public void Success(string message) => writer.WriteLine($"[OK] {message}");

    public void Warning(string message) => writer.WriteLine($"[WARN] {message}");

    public void Error(string message) => writer.WriteLine($"[ERROR] {message}");

    public void Debug(string message) => writer.WriteLine($"[DEBUG] {message}");

    public void Section(string title)
    {
        writer.WriteLine(title);
        writer.WriteLine(new string('-', title.Length));
    }

    public void DisplayBanner(string version)
    {
        writer.WriteLine("DevSweep");
        writer.WriteLine($"Version {version}");
    }

    public void DisplayAnalysisReport(AnalysisReport report)
    {
        writer.WriteLine($"Analysis Report — {report.ModuleCount()} module(s), {report.TotalItemCount()} item(s), {report.TotalSize()}");
        writer.WriteLine(new string('-', 60));

        foreach (var module in report.Modules())
        {
            writer.WriteLine($"  {module.Module,-16} items={module.ItemCount(),4}  safe={module.SafeItemCount(),4}  unsafe={module.UnsafeItemCount(),4}  size={module.TotalSize()}");
        }
    }

    public void DisplayCompletion(IReadOnlyList<CleanupSummary> summaries)
    {
        var totalFiles = summaries.Sum(s => s.Result.TotalFilesDeleted());
        var totalSize = summaries.Aggregate(
            FileSize.Zero,
            (acc, s) => acc.Add(s.Result.TotalSpaceFreed()));

        writer.WriteLine($"Cleanup Complete — {totalFiles} file(s) deleted, {totalSize} freed");
        writer.WriteLine(new string('-', 60));

        foreach (var summary in summaries)
        {
            writer.WriteLine($"  {summary.Module,-16} deleted={summary.Result.TotalFilesDeleted(),4}  freed={summary.Result.TotalSpaceFreed()}  errors={summary.Result.ErrorMessages().Count}");
        }
    }
}
