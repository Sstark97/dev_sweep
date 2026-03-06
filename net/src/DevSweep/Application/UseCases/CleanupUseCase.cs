using DevSweep.Application.Modules;
using DevSweep.Application.Ports.Driven;
using DevSweep.Application.Ports.Driving;
using DevSweep.Domain.Common;
using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;

namespace DevSweep.Application.UseCases;

public sealed class CleanupUseCase(
    ModuleRegistry registry,
    IOutputFormatter outputFormatter,
    IUserInteraction userInteraction
) : ICleanupUseCase
{
    private readonly IReadOnlyList<CleanupSummary> emptySummary = [];

    public async Task<Result<IReadOnlyList<CleanupSummary>, DomainError>> Invoke(
        IReadOnlyList<CleanupModuleName> modules,
        CancellationToken cancellationToken)
    {
        if (modules.Count == 0)
            return Result<IReadOnlyList<CleanupSummary>, DomainError>.Success(emptySummary);

        var resolveResult = ResolveModules(modules);
        if (resolveResult.IsFailure)
            return Result<IReadOnlyList<CleanupSummary>, DomainError>.Failure(resolveResult.Error);

        var confirmed = await UserConfirmedAsync(resolveResult.Value, cancellationToken);
        if (!confirmed)
            return Result<IReadOnlyList<CleanupSummary>, DomainError>.Success(emptySummary);

        var cleanResult = await CleanModulesAsync(resolveResult.Value, cancellationToken);
        if (cleanResult.IsFailure)
            return Result<IReadOnlyList<CleanupSummary>, DomainError>.Failure(cleanResult.Error);

        outputFormatter.DisplayCompletion(cleanResult.Value);

        return Result<IReadOnlyList<CleanupSummary>, DomainError>.Success(cleanResult.Value);
    }

    private Result<List<ICleanupModule>, DomainError> ResolveModules(
        IReadOnlyList<CleanupModuleName> modules)
    {
        var resolvedModules = new List<ICleanupModule>();

        foreach (var name in modules)
        {
            var moduleResult = registry.ForName(name);
            if (moduleResult.IsFailure)
                return Result<List<ICleanupModule>, DomainError>.Failure(moduleResult.Error);

            resolvedModules.Add(moduleResult.Value);
        }

        return Result<List<ICleanupModule>, DomainError>.Success(resolvedModules);
    }

    private async Task<bool> UserConfirmedAsync(
        IReadOnlyList<ICleanupModule> modules, CancellationToken cancellationToken)
    {
        if (!modules.Any(m => m.IsDestructive))
            return true;

        return await userInteraction.ConfirmAsync(
            "This operation includes destructive modules. Are you sure you want to proceed?",
            isDestructive: true,
            cancellationToken);
    }

    private static async Task<Result<List<CleanupSummary>, DomainError>> CleanModulesAsync(
        IReadOnlyList<ICleanupModule> modules, CancellationToken cancellationToken)
    {
        var summaries = new List<CleanupSummary>();

        foreach (var module in modules)
        {
            var analysisResult = await module.AnalyzeAsync(cancellationToken);
            if (analysisResult.IsFailure)
                return Result<List<CleanupSummary>, DomainError>.Failure(analysisResult.Error);

            var analysis = analysisResult.Value;

            if (analysis.IsEmpty())
                continue;

            var cleanResult = await module.CleanAsync(analysis.Items(), cancellationToken);
            if (cleanResult.IsFailure)
                return Result<List<CleanupSummary>, DomainError>.Failure(cleanResult.Error);

            var summaryResult = CleanupSummary.Create(module.Name, analysis.Items(), cleanResult.Value);
            if (summaryResult.IsFailure)
                return Result<List<CleanupSummary>, DomainError>.Failure(summaryResult.Error);

            summaries.Add(summaryResult.Value);
        }

        return Result<List<CleanupSummary>, DomainError>.Success(summaries);
    }
}
