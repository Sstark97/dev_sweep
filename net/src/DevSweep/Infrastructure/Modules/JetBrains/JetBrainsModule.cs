using DevSweep.Application.Models;
using DevSweep.Application.Modules;
using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Common;
using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Infrastructure.Modules.JetBrains;

public sealed class JetBrainsModule(
    IFileSystem fileSystem,
    IProcessManager processManager,
    IEnvironmentProvider environment) : ICleanupModule
{
    private static readonly string[] Products =
    [
        "Rider", "IntelliJIdea", "WebStorm", "DataGrip", "RustRover",
        "IdeaIC", "JetBrainsClient", "PyCharm", "GoLand", "CLion", "PhpStorm"
    ];

    private static readonly string[] ProcessNames =
    [
        "Rider", "IntelliJ", "WebStorm", "DataGrip", "RustRover",
        "PyCharm", "GoLand", "CLion", "PhpStorm"
    ];

    private const int KeepLatestCount = 1;

    public CleanupModuleName Name => CleanupModuleName.JetBrains;
    public string Description => "Clean JetBrains IDEs (old versions & caches)";
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
        var basePath = environment.JetBrainsBasePath();

        if (!fileSystem.DirectoryExists(basePath))
            return ModuleAnalysis.Create(CleanupModuleName.JetBrains, []);

        var childrenResult = await FindImmediateChildrenAsync(basePath, cancellationToken);

        return
            from children in childrenResult
            from candidates in CollectCleanupCandidates(children)
            from analysis in ModuleAnalysis.Create(CleanupModuleName.JetBrains, candidates)
            select analysis;
    }

    public async Task<Result<CleanupResult, DomainError>> CleanAsync(
        IReadOnlyList<CleanableItem> artifactsToClean, CancellationToken cancellationToken)
    {
        var processErrors = await ShutdownRunningProcessesAsync(cancellationToken);
        var deleteResult = await DeleteSafeItemsAsync(artifactsToClean, cancellationToken);

        if (processErrors.Count > 0)
        {
            var processErrorResult = CleanupResult.CreateWithErrors(0, FileSize.Zero, processErrors).Value;
            deleteResult = deleteResult.Combine(processErrorResult);
        }

        return Result<CleanupResult, DomainError>.Success(deleteResult);
    }

    private Result<IReadOnlyList<CleanableItem>, DomainError> CollectCleanupCandidates(
        IReadOnlyList<FilePath> children)
    {
        var cacheItem = BuildCacheItem();

        return
            from outdated in Products.CollectMany(product => BuildOutdatedItemsForProduct(product, children))
            let withCache = cacheItem.Match(
                cache => [.. outdated, cache],
                () => outdated)
            select withCache;
    }

    private async Task<Result<IReadOnlyList<FilePath>, DomainError>> FindImmediateChildrenAsync(
        FilePath basePath, CancellationToken cancellationToken)
    {
        var findResult = await fileSystem.FindDirectoriesAsync(basePath, "*", cancellationToken);

        return findResult.Map(dirs =>
            (IReadOnlyList<FilePath>)[.. from dir in dirs
                where IsImmediateChild(basePath, dir)
                select dir]);
    }

    private Result<IReadOnlyList<CleanableItem>, DomainError> BuildOutdatedItemsForProduct(
        string product, IReadOnlyList<FilePath> candidates)
    {
        var productDirs = (from dir in candidates
            where dir.FileName().StartsWith(product, StringComparison.OrdinalIgnoreCase)
            let version = CleanupItemVersion.Create(dir.FileName()[product.Length..])
            where version.IsSuccess
            orderby version.Value
            select dir).ToList();

        if (productDirs.Count <= KeepLatestCount)
            return Result<IReadOnlyList<CleanableItem>, DomainError>.Success([]);

        var outdated = productDirs.Take(productDirs.Count - KeepLatestCount);

        return outdated.Collect(dir =>
            from size in fileSystem.Size(dir)
            select CleanableItem.CreateSafe(
                dir,
                size,
                CleanupModuleName.JetBrains,
                $"Outdated JetBrains IDE version: {dir.FileName()}"));
    }

    private Option<CleanableItem> BuildCacheItem()
    {
        var cachePath = environment.JetBrainsCachePath();

        if (!fileSystem.DirectoryExists(cachePath) || !fileSystem.IsDirectoryNotEmpty(cachePath))
            return Option<CleanableItem>.None;

        return
            from size in fileSystem.Size(cachePath).ToOption()
            select CleanableItem.CreateSafe(
                cachePath,
                size,
                CleanupModuleName.JetBrains,
                "JetBrains IDE index caches");
    }

    private async Task<List<string>> ShutdownRunningProcessesAsync(CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var runningProcesses = from name in ProcessNames
            where processManager.IsProcessRunning(name)
            select name;

        foreach (var processName in runningProcesses)
        {
            var killResult = await processManager.KillProcessAsync(processName, cancellationToken);

            if (killResult.IsFailure)
                errors.Add($"Failed to kill process {processName}: {killResult.Error}");
        }

        return errors;
    }

    private async Task<CleanupResult> DeleteSafeItemsAsync(
        IReadOnlyList<CleanableItem> artifactsToClean, CancellationToken cancellationToken)
    {
        var accumulated = CleanupResult.Empty;
        var safeArtifactsToClean = from artifact in artifactsToClean
            where artifact.IsSafeToDelete
            select artifact;

        foreach (var artifact in safeArtifactsToClean)
        {
            var deleteResult = await fileSystem.DeleteDirectoryAsync(artifact.Path, cancellationToken);

            if (deleteResult.IsFailure)
            {
                var errorItem = CleanupResult.CreateWithErrors(0, FileSize.Zero, [$"Failed to delete {artifact.Path}: {deleteResult.Error}"]).Value;
                accumulated = accumulated.Combine(errorItem);
                continue;
            }

            accumulated = accumulated.Combine(CleanupResult.Create(1, artifact.Size).Value);
        }

        return accumulated;
    }

    private static bool IsImmediateChild(FilePath basePath, FilePath childPath)
    {
        var baseNormalized = basePath.ToString()
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var parent = Path.GetDirectoryName(childPath.ToString())
            ?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return string.Equals(parent, baseNormalized, StringComparison.OrdinalIgnoreCase);
    }

}
