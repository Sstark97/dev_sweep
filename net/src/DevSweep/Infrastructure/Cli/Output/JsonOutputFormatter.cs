using System.Text.Json;
using DevSweep.Application.Models;
using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Entities;
using DevSweep.Infrastructure.Cli.Output.JsonModels;

namespace DevSweep.Infrastructure.Cli.Output;

public sealed class JsonOutputFormatter(TextWriter writer) : IOutputFormatter
{
    public void Info(string message) =>
        WriteLine(JsonSerializer.Serialize(new LogLineJson("info", message), DevSweepJsonContext.Default.LogLineJson));

    public void Success(string message) =>
        WriteLine(JsonSerializer.Serialize(new LogLineJson("success", message), DevSweepJsonContext.Default.LogLineJson));

    public void Warning(string message) =>
        WriteLine(JsonSerializer.Serialize(new LogLineJson("warning", message), DevSweepJsonContext.Default.LogLineJson));

    public void Error(string message) =>
        WriteLine(JsonSerializer.Serialize(new LogLineJson("error", message), DevSweepJsonContext.Default.LogLineJson));

    public void Debug(string message) =>
        WriteLine(JsonSerializer.Serialize(new LogLineJson("debug", message), DevSweepJsonContext.Default.LogLineJson));

    public void Section(string title) =>
        WriteLine(JsonSerializer.Serialize(new SectionJson("section", title), DevSweepJsonContext.Default.SectionJson));

    public void DisplayBanner(string version) =>
        WriteLine(JsonSerializer.Serialize(new BannerJson("banner", version), DevSweepJsonContext.Default.BannerJson));

    public void DisplayAnalysisReport(AnalysisReport report) =>
        WriteLine(JsonSerializer.Serialize(report.ToJson(), DevSweepJsonContext.Default.AnalysisReportJson));

    public void DisplayCompletion(IReadOnlyList<CleanupSummary> summaries) =>
        WriteLine(JsonSerializer.Serialize(summaries.ToJson(), DevSweepJsonContext.Default.CleanupCompletionJson));

    private void WriteLine(string line) => writer.WriteLine(line);
}
