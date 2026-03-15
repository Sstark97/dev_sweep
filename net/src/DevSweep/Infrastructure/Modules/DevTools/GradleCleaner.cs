using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Common;
using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Infrastructure.Modules.DevTools;

public sealed class GradleCleaner(
    IFileSystem fileSystem,
    IEnvironmentProvider environment) : IDevToolsCleaner
{
    private const string CacheReason = "devtools:gradle-cache";
    private const string WrapperNuclearReason = "devtools:gradle-wrapper-nuclear";

    public string CleanerName => "Gradle";

    public bool IsAvailable(OperatingSystemType operatingSystem) => true;

    public Task<Result<IReadOnlyList<CleanableItem>, DomainError>> AnalyzeAsync(CancellationToken cancellationToken)
    {
        var cacheItem = BuildCacheItem();
        var wrapperItem = BuildWrapperItem();

        List<CleanableItem> items = [.. new[] { cacheItem, wrapperItem }.SelectMany(opt => opt.ToEnumerable())];

        return Task.FromResult(Result<IReadOnlyList<CleanableItem>, DomainError>.Success((IReadOnlyList<CleanableItem>)items));
    }

    public async Task<Result<CleanupResult, DomainError>> CleanAsync(
        IReadOnlyList<CleanableItem> items, CancellationToken cancellationToken)
    {
        var accumulated = CleanupResult.Empty;
        var ownItems = from item in items
            where item.Reason is CacheReason or WrapperNuclearReason
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

    private Option<CleanableItem> BuildCacheItem()
    {
        var cachePath = environment.GradleCachePath();

        if (!fileSystem.DirectoryExists(cachePath) || !fileSystem.IsDirectoryNotEmpty(cachePath))
            return Option<CleanableItem>.None;

        return
            from size in fileSystem.Size(cachePath).ToOption()
            select CleanableItem.CreateSafe(cachePath, size, CleanupModuleName.DevTools, CacheReason);
    }

    private Option<CleanableItem> BuildWrapperItem()
    {
        var wrapperPath = environment.GradleWrapperPath();

        if (!fileSystem.DirectoryExists(wrapperPath) || !fileSystem.IsDirectoryNotEmpty(wrapperPath))
            return Option<CleanableItem>.None;

        return
            from size in fileSystem.Size(wrapperPath).ToOption()
            select CleanableItem.CreateUnsafe(wrapperPath, size, CleanupModuleName.DevTools, WrapperNuclearReason);
    }

}
