using DevSweep.Application.Models;
using DevSweep.Application.Modules;
using DevSweep.Application.Ports.Driven;
using DevSweep.Application.Ports.Driving;
using DevSweep.Domain.Common;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;

namespace DevSweep.Application.UseCases;

public sealed class AnalyzeUseCase(
    ModuleRegistry registry,
    IOutputFormatter outputFormatter
) : IAnalyzeUseCase
{
    public async Task<Result<AnalysisReport, DomainError>> Invoke(
        IReadOnlyList<CleanupModuleName> modules,
        CancellationToken cancellationToken)
    {
        if (modules.Count == 0)
            return Result<AnalysisReport, DomainError>.Success(AnalysisReport.CreateEmpty());

        var analysesResult = await AnalyzeModulesAsync(modules, cancellationToken);
        if (analysesResult.IsFailure)
            return Result<AnalysisReport, DomainError>.Failure(analysesResult.Error);

        var reportResult = AnalysisReport.Create(analysesResult.Value);
        if (reportResult.IsFailure)
            return Result<AnalysisReport, DomainError>.Failure(reportResult.Error);

        outputFormatter.DisplayAnalysisReport(reportResult.Value);

        return Result<AnalysisReport, DomainError>.Success(reportResult.Value);
    }

    private async Task<Result<List<ModuleAnalysis>, DomainError>> AnalyzeModulesAsync(
        IReadOnlyList<CleanupModuleName> modules, CancellationToken cancellationToken)
    {
        var moduleAnalyses = new List<ModuleAnalysis>();

        foreach (var name in modules)
        {
            var moduleResult = registry.ForName(name);
            if (moduleResult.IsFailure)
                return Result<List<ModuleAnalysis>, DomainError>.Failure(moduleResult.Error);

            var analysisResult = await moduleResult.Value.AnalyzeAsync(cancellationToken);
            if (analysisResult.IsFailure)
                return Result<List<ModuleAnalysis>, DomainError>.Failure(analysisResult.Error);

            moduleAnalyses.Add(analysisResult.Value);
        }

        return Result<List<ModuleAnalysis>, DomainError>.Success(moduleAnalyses);
    }
}
