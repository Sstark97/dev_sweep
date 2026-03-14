using DevSweep.Application.Models;
using DevSweep.Application.Modules;
using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Common;
using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Infrastructure.Modules.Homebrew;

public sealed class HomebrewModule(
    ICommandRunner commandRunner,
    IFileSystem fileSystem,
    IEnvironmentProvider environment) : ICleanupModule
{
    private const long BytesPerMegabyte = 1024L * 1024L;
    private const long OutdatedPackageEstimateMegabytes = 50L;
    private const long UnusedDepEstimateMegabytes = 20L;
    private const string DownloadCacheReason = "homebrew:download-cache";
    private const string OutdatedPackagesReason = "homebrew:outdated-packages";
    private const string UnusedDepsReason = "homebrew:unused-deps";

    public CleanupModuleName Name => CleanupModuleName.Homebrew;
    public string Description => "Clean Homebrew (outdated packages, unused deps, download cache)";
    public bool IsDestructive => true;

    public bool IsAvailableOnPlatform(OperatingSystemType operatingSystem) => operatingSystem switch
    {
        OperatingSystemType.MacOS => true,
        OperatingSystemType.Linux => true,
        _ => false
    };

    public async Task<Result<ModuleAnalysis, DomainError>> AnalyzeAsync(CancellationToken cancellationToken)
    {
        if (!commandRunner.IsCommandAvailable("brew"))
            return ModuleAnalysis.Create(CleanupModuleName.Homebrew, []);

        var outdatedTask = EstimateOutdatedPackagesAsync(cancellationToken);
        var unusedDepsTask = EstimateUnusedDepsAsync(cancellationToken);

        var results = await Task.WhenAll(outdatedTask, unusedDepsTask);

        var cacheItem = BuildDownloadCacheItem();

        List<CleanableItem> items = [..
            new[] { cacheItem, results[0], results[1] }
                .SelectMany(opt => opt.Match<IEnumerable<CleanableItem>>(item => [item], () => []))];

        return ModuleAnalysis.Create(CleanupModuleName.Homebrew, items);
    }

    public async Task<Result<CleanupResult, DomainError>> CleanAsync(
        IReadOnlyList<CleanableItem> artifactsToClean, CancellationToken cancellationToken)
    {
        if (artifactsToClean.Count == 0 || !commandRunner.IsCommandAvailable("brew"))
            return Result<CleanupResult, DomainError>.Success(CleanupResult.Empty);

        var cleanupItems = ItemsWithReason(artifactsToClean, DownloadCacheReason)
            .Concat(ItemsWithReason(artifactsToClean, OutdatedPackagesReason))
            .ToList();

        var unusedDepsItems = ItemsWithReason(artifactsToClean, UnusedDepsReason);

        var cleanupResult = await RunBrewCommandAsync("cleanup --prune=all", "Brew cleanup failed", cleanupItems, cancellationToken);
        var autoremoveResult = await RunBrewCommandAsync("autoremove", "Brew autoremove failed", unusedDepsItems, cancellationToken);

        return
            from cleanup in cleanupResult
            from autoremove in autoremoveResult
            select cleanup.Combine(autoremove);
    }

    private Option<CleanableItem> BuildDownloadCacheItem()
    {
        var cachePath = environment.HomebrewCachePath();

        if (!fileSystem.DirectoryExists(cachePath) || !fileSystem.IsDirectoryNotEmpty(cachePath))
            return Option<CleanableItem>.None;

        return
            from size in fileSystem.Size(cachePath).ToOption()
            select CleanableItem.CreateSafe(cachePath, size, CleanupModuleName.Homebrew, DownloadCacheReason);
    }

    private async Task<Option<CleanableItem>> EstimateOutdatedPackagesAsync(CancellationToken cancellationToken)
    {
        var result = await commandRunner.RunAsync("brew", "cleanup --dry-run", cancellationToken);
        var bytes = EstimateFromOutput(result, CountLines, OutdatedPackageEstimateMegabytes);

        if (bytes == 0)
            return Option<CleanableItem>.None;

        var cachePath = environment.HomebrewCachePath();
        return
            from size in FileSize.Create(bytes).ToOption()
            select CleanableItem.CreateSafe(cachePath, size, CleanupModuleName.Homebrew, OutdatedPackagesReason);
    }

    private async Task<Option<CleanableItem>> EstimateUnusedDepsAsync(CancellationToken cancellationToken)
    {
        var result = await commandRunner.RunAsync("brew", "autoremove --dry-run", cancellationToken);
        var bytes = EstimateFromOutput(result, CountWouldRemoveLines, UnusedDepEstimateMegabytes);

        if (bytes == 0)
            return Option<CleanableItem>.None;

        var cachePath = environment.HomebrewCachePath();
        return
            from size in FileSize.Create(bytes).ToOption()
            select CleanableItem.CreateSafe(cachePath, size, CleanupModuleName.Homebrew, UnusedDepsReason);
    }

    private async Task<Result<CleanupResult, DomainError>> RunBrewCommandAsync(
        string args, string errorLabel, IReadOnlyList<CleanableItem> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0)
            return Result<CleanupResult, DomainError>.Success(CleanupResult.Empty);

        var commandResult = await commandRunner.RunAsync("brew", args, cancellationToken);

        if (!commandResult.Match(output => output.IsSuccessful(), _ => false))
            return CleanupResult.CreateWithErrors(0, FileSize.Zero,
                [$"{errorLabel}: {CommandOutput.ErrorMessage(commandResult)}"]);

        var freed = items.Aggregate(FileSize.Zero, (acc, item) => acc.Add(item.Size));
        return CleanupResult.Create(items.Count, freed);
    }

    private static long EstimateFromOutput(
        Result<CommandOutput, DomainError> result,
        Func<string, int> counter,
        long megabytesPerItem) =>
        result.ToOption()
            .Filter(output => output.IsSuccessful())
            .Map(output => counter(output.StandardOutput()) * megabytesPerItem * BytesPerMegabyte)
            .ValueOr(0L);

    private static IReadOnlyList<CleanableItem> ItemsWithReason(
        IReadOnlyList<CleanableItem> items, string reason) =>
        [.. from item in items where item.Reason == reason select item];

    private static int CountLines(string output) =>
        output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;

    private static int CountWouldRemoveLines(string output) =>
        output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Count(line => line.Contains("Would remove", StringComparison.OrdinalIgnoreCase));
}
