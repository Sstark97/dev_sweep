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

        var duplicates = from analysis in moduleAnalyses
                         group analysis by analysis.Module into g
                         where g.Count() > 1
                         select g.Key;

        if (duplicates.Any())
            return Result<AnalysisReport, DomainError>.Failure(
                DomainError.Validation("Duplicate module analyses are not allowed"));

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
