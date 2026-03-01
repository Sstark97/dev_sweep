using DevSweep.Domain.Common;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Application.Models;

public sealed record AnalysisReport
{
    private AnalysisReport(IReadOnlyList<ModuleAnalysis> moduleAnalyses)
    {
        ModuleAnalyses = moduleAnalyses;
    }

    public static Result<AnalysisReport, DomainError> Create(IReadOnlyList<ModuleAnalysis>? moduleAnalyses)
    {
        if (moduleAnalyses is null)
            return Result<AnalysisReport, DomainError>.Failure(
                DomainError.Validation("moduleAnalyses is required"));

        return Result<AnalysisReport, DomainError>.Success(
            new AnalysisReport(moduleAnalyses));
    }

    public static AnalysisReport CreateEmpty() =>
        new([]);

    private IReadOnlyList<ModuleAnalysis> ModuleAnalyses { get; }

    public FileSize TotalSize()
    {
        var zero = FileSize.Create(0).Value;
        return ModuleAnalyses.Aggregate(zero, (acc, analysis) => acc.Add(analysis.TotalSize()));
    }

    public int TotalItemCount() => ModuleAnalyses.Sum(analysis => analysis.ItemCount());

    public int ModuleCount() => ModuleAnalyses.Count;

    public bool IsEmpty() => ModuleAnalyses.All(analysis => analysis.IsEmpty());
}
