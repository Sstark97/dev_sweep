using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Common;
using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Infrastructure.Modules.DevTools;

public sealed class NodeCleaner(
    IFileSystem fileSystem,
    IEnvironmentProvider environment) : IDevToolsCleaner
{
    private const string NpmCacheReason = "devtools:npm-cache";
    private const string YarnCacheReason = "devtools:yarn-cache";
    private const string PnpmStoreReason = "devtools:pnpm-store";
    private const string NvmCacheReason = "devtools:nvm-cache";
    private const string NpmDirectoryNuclearReason = "devtools:npm-directory-nuclear";

    private static readonly HashSet<string> OwnReasons =
        [NpmCacheReason, YarnCacheReason, PnpmStoreReason, NvmCacheReason, NpmDirectoryNuclearReason];

    public string CleanerName => "Node";

    public bool IsAvailable(OperatingSystemType operatingSystem) => true;

    public Task<Result<IReadOnlyList<CleanableItem>, DomainError>> AnalyzeAsync(CancellationToken cancellationToken)
    {
        var npmCacheItem = BuildSafeItem(environment.NodeModulesGlobalPath(), NpmCacheReason);
        var yarnCacheItem = BuildSafeItem(environment.YarnCachePath(), YarnCacheReason);
        var pnpmStoreItem = BuildSafeItem(environment.PnpmStorePath(), PnpmStoreReason);
        var nvmCacheItem = BuildSafeItem(environment.NvmCachePath(), NvmCacheReason);
        var npmDirectoryItem = BuildUnsafeItem(environment.NpmFullPath(), NpmDirectoryNuclearReason);

        List<CleanableItem> items = [..
            new[] { npmCacheItem, yarnCacheItem, pnpmStoreItem, nvmCacheItem, npmDirectoryItem }
                .SelectMany(opt => opt.ToEnumerable())];

        return Task.FromResult(Result<IReadOnlyList<CleanableItem>, DomainError>.Success((IReadOnlyList<CleanableItem>)items));
    }

    public async Task<Result<CleanupResult, DomainError>> CleanAsync(
        IReadOnlyList<CleanableItem> items, CancellationToken cancellationToken)
    {
        var accumulated = CleanupResult.Empty;
        var ownItems = from item in items where item.Reason is not null && OwnReasons.Contains(item.Reason) select item;

        foreach (var item in ownItems)
        {
            var deleteResult = await fileSystem.DeleteDirectoryAsync(item.Path, cancellationToken);

            var itemResult = deleteResult.Match(
                _ => CleanupResult.Create(1, item.Size).Recover(CleanupResult.Empty).Value,
                error => CleanupResult.CreateWithErrors(0, FileSize.Zero,
                    [$"Failed to delete {item.Path}: {error}"]).Recover(CleanupResult.Empty).Value);

            accumulated = accumulated.Combine(itemResult);
        }

        return Result<CleanupResult, DomainError>.Success(accumulated);
    }

    private Option<CleanableItem> BuildSafeItem(FilePath path, string reason)
    {
        if (!fileSystem.DirectoryExists(path) || !fileSystem.IsDirectoryNotEmpty(path))
            return Option<CleanableItem>.None;

        return
            from size in fileSystem.Size(path).ToOption()
            select CleanableItem.CreateSafe(path, size, CleanupModuleName.DevTools, reason);
    }

    private Option<CleanableItem> BuildUnsafeItem(FilePath path, string reason)
    {
        if (!fileSystem.DirectoryExists(path) || !fileSystem.IsDirectoryNotEmpty(path))
            return Option<CleanableItem>.None;

        return
            from size in fileSystem.Size(path).ToOption()
            select CleanableItem.CreateUnsafe(path, size, CleanupModuleName.DevTools, reason);
    }

}
