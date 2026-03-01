using DevSweep.Application.Models;
using DevSweep.Domain.Entities;

namespace DevSweep.Application.Ports.Driven;

public interface IOutputFormatter
{
    void Info(string message);
    void Success(string message);
    void Warning(string message);
    void Error(string message);
    void Debug(string message);
    void Section(string title);
    void DisplayAnalysisReport(AnalysisReport report);
    void DisplayBanner(string version);
    void DisplayCompletion(IReadOnlyList<CleanupSummary> summaries);
}
