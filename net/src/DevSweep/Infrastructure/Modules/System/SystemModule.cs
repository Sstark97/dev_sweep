using DevSweep.Application.Models;
using DevSweep.Application.Modules;
using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Common;
using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Infrastructure.Modules.System;

public sealed class SystemModule(
    IEnvironmentProvider environment,
    IReadOnlyList<ISystemCleaner> cleaners) : ICleanupModule
{
    public CleanupModuleName Name => CleanupModuleName.System;
    public string Description => "Clean system caches, logs, and temp files";
    public bool IsDestructive => false;

    public bool IsAvailableOnPlatform(OperatingSystemType operatingSystem) => operatingSystem switch
    {
        OperatingSystemType.MacOS => true,
        OperatingSystemType.Linux => true,
        OperatingSystemType.Windows => true,
        _ => false
    };

    public async Task<Result<ModuleAnalysis, DomainError>> AnalyzeAsync(CancellationToken cancellationToken)
    {
        var analysisResults = await Task.WhenAll(
            ActiveCleaners().Select(c => c.AnalyzeAsync(cancellationToken)));

        List<CleanableItem> allItems = [.. from result in analysisResults
            where result.IsSuccess
            from item in result.Value
            select item];

        return ModuleAnalysis.Create(CleanupModuleName.System, allItems);
    }

    public async Task<Result<CleanupResult, DomainError>> CleanAsync(
        IReadOnlyList<CleanableItem> artifactsToClean, CancellationToken cancellationToken)
    {
        if (artifactsToClean.Count == 0)
            return Result<CleanupResult, DomainError>.Success(CleanupResult.Empty);

        var cleanResults = await Task.WhenAll(
            ActiveCleaners().Select(c => c.CleanAsync(artifactsToClean, cancellationToken)));

        return
            from results in cleanResults.ToList().Collect(r => r)
            select results.Aggregate(CleanupResult.Empty, (acc, r) => acc.Combine(r));
    }

    private IEnumerable<ISystemCleaner> ActiveCleaners() =>
        from cleaner in cleaners
        where cleaner.IsAvailable(environment.CurrentOperatingSystem)
        select cleaner;
}
