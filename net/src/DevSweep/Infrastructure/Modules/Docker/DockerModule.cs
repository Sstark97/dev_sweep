using DevSweep.Application.Models;
using DevSweep.Application.Modules;
using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Common;
using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Infrastructure.Modules.Docker;

public sealed class DockerModule(
    IEnvironmentProvider environment,
    IReadOnlyList<IContainerRuntimeStrategy> strategies) : ICleanupModule
{
    public CleanupModuleName Name => CleanupModuleName.Docker;
    public string Description => "Clean Docker/OrbStack (containers, images, volumes, networks)";
    public bool IsDestructive => true;

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
            ActiveStrategies().Select(s => s.AnalyzeAsync(cancellationToken)));

        List<CleanableItem> allItems = [.. from result in analysisResults
            where result.IsSuccess
            from item in result.Value
            select item];

        return ModuleAnalysis.Create(CleanupModuleName.Docker, allItems);
    }

    public async Task<Result<CleanupResult, DomainError>> CleanAsync(
        IReadOnlyList<CleanableItem> artifactsToClean, CancellationToken cancellationToken)
    {
        if (artifactsToClean.Count == 0)
            return Result<CleanupResult, DomainError>.Success(CleanupResult.Empty);

        var cleanResults = await Task.WhenAll(
            ActiveStrategies().Select(s => s.CleanAsync(artifactsToClean, cancellationToken)));

        return
            from results in cleanResults.ToList().Collect(r => r)
            select results.Aggregate(CleanupResult.Empty, (acc, r) => acc.Combine(r));
    }

    private IEnumerable<IContainerRuntimeStrategy> ActiveStrategies() =>
        strategies.Where(s => s.IsAvailable(environment.CurrentOperatingSystem));
}
