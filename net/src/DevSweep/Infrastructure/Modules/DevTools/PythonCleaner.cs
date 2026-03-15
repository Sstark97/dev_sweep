using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Common;
using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Infrastructure.Modules.DevTools;

public sealed class PythonCleaner(
    IFileSystem fileSystem,
    IEnvironmentProvider environment) : IDevToolsCleaner
{
    private const string PipCacheReason = "devtools:pip-cache";
    private const string PoetryCacheReason = "devtools:poetry-cache";

    public string CleanerName => "Python";

    public bool IsAvailable(OperatingSystemType operatingSystem) => true;

    public Task<Result<IReadOnlyList<CleanableItem>, DomainError>> AnalyzeAsync(CancellationToken cancellationToken)
    {
        var pipCacheItem = BuildItem(environment.PythonCachePath(), PipCacheReason);
        var poetryCacheItem = BuildItem(environment.PoetryCachePath(), PoetryCacheReason);

        List<CleanableItem> items = [.. new[] { pipCacheItem, poetryCacheItem }.SelectMany(opt => opt.ToEnumerable())];

        return Task.FromResult(Result<IReadOnlyList<CleanableItem>, DomainError>.Success((IReadOnlyList<CleanableItem>)items));
    }

    public async Task<Result<CleanupResult, DomainError>> CleanAsync(
        IReadOnlyList<CleanableItem> items, CancellationToken cancellationToken)
    {
        var accumulated = CleanupResult.Empty;
        var ownItems = from item in items
            where item.Reason is PipCacheReason or PoetryCacheReason
            select item;

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

    private Option<CleanableItem> BuildItem(FilePath path, string reason)
    {
        if (!fileSystem.DirectoryExists(path) || !fileSystem.IsDirectoryNotEmpty(path))
            return Option<CleanableItem>.None;

        return
            from size in fileSystem.Size(path).ToOption()
            select CleanableItem.CreateSafe(path, size, CleanupModuleName.DevTools, reason);
    }

}
