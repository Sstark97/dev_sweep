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
        var (deletedCount, freedBytes, deleteErrors) = await DeleteSafeItemsAsync(artifactsToClean, cancellationToken);

        var allErrors = processErrors.Concat(deleteErrors).ToList();

        if (allErrors.Count > 0)
            return CleanupResult.CreateWithErrors(deletedCount, freedBytes, allErrors);

        return CleanupResult.Create(deletedCount, freedBytes);
    }

    private Result<IReadOnlyList<CleanableItem>, DomainError> CollectCleanupCandidates(
        IReadOnlyList<FilePath> children)
        =>
            from outdated in Products.CollectMany(product => BuildOutdatedItemsForProduct(product, children))
            from cache in BuildCacheItem()
            select cache is not null
                ? (IReadOnlyList<CleanableItem>)[.. outdated, cache]
                : outdated;

    private async Task<Result<IReadOnlyList<FilePath>, DomainError>> FindImmediateChildrenAsync(
        FilePath basePath, CancellationToken cancellationToken)
    {
        var findResult = await fileSystem.FindDirectoriesAsync(basePath, "*", cancellationToken);

        if (findResult.IsFailure)
            return Result<IReadOnlyList<FilePath>, DomainError>.Failure(findResult.Error);

        var immediateChildren = findResult.Value
            .Where(dir => IsImmediateChild(basePath, dir))
            .ToList();

        return Result<IReadOnlyList<FilePath>, DomainError>.Success(immediateChildren);
    }

    private Result<IReadOnlyList<CleanableItem>, DomainError> BuildOutdatedItemsForProduct(
        string product, IReadOnlyList<FilePath> candidates)
    {
        var productDirs = candidates
            .Where(dir => dir.FileName().StartsWith(product, StringComparison.OrdinalIgnoreCase))
            .Select(dir => new { dir, version = CleanupItemVersion.Create(dir.FileName()[product.Length..]) })
            .Where(x => x.version.IsSuccess)
            .OrderBy(x => x.version.Value)
            .Select(x => x.dir)
            .ToList();

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

    private Result<CleanableItem?, DomainError> BuildCacheItem()
    {
        var cachePath = environment.JetBrainsCachePath();

        if (!fileSystem.DirectoryExists(cachePath) || !fileSystem.IsDirectoryNotEmpty(cachePath))
            return Result<CleanableItem?, DomainError>.Success(null);

        return
            from size in fileSystem.Size(cachePath)
            select (CleanableItem?)CleanableItem.CreateSafe(
                cachePath,
                size,
                CleanupModuleName.JetBrains,
                "JetBrains IDE index caches");
    }

    private async Task<List<string>> ShutdownRunningProcessesAsync(CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var runningProcesses = ProcessNames.Where(processManager.IsProcessRunning);

        foreach (var processName in runningProcesses)
        {
            var killResult = await processManager.KillProcessAsync(processName, cancellationToken);

            if (killResult.IsFailure)
                errors.Add($"Failed to kill process {processName}: {killResult.Error}");
        }

        return errors;
    }

    private async Task<(int deletedCount, FileSize freedBytes, List<string> errors)> DeleteSafeItemsAsync(
        IReadOnlyList<CleanableItem> artifactsToClean, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var deletedCount = 0;
        var freedBytes = FileSize.Create(0).Value;
        var safeArtifactsToClean = artifactsToClean.Where(artifact => artifact.IsSafeToDelete);

        foreach (var artifact in safeArtifactsToClean)
        {
            var deleteResult = await fileSystem.DeleteDirectoryAsync(artifact.Path, cancellationToken);

            if (deleteResult.IsFailure)
            {
                errors.Add($"Failed to delete {artifact.Path}: {deleteResult.Error}");
                continue;
            }

            deletedCount++;
            freedBytes = freedBytes.Add(artifact.Size);
        }

        return (deletedCount, freedBytes, errors);
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
