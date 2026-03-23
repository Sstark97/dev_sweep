using System.Collections.Frozen;
using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Common;
using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Infrastructure.Modules.System;

public sealed class SystemCacheCleaner(
    IFileSystem fileSystem,
    IEnvironmentProvider environment) : ISystemCleaner
{
    private const int StaleThresholdDays = 30;
    private const string CacheSubdirectoryReason = "system:user-cache";

    private static readonly HashSet<string> OwnReasons = [CacheSubdirectoryReason];

    private static readonly FrozenSet<string> SkipDirectories = FrozenSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "com.apple.bird",
        "CloudKit",
        "com.apple.Safari",
        "Google",
        "Firefox",
        "Spotlight",
        "com.apple.nsurlsessiond",
        "google-chrome",
        "mozilla",
        "chromium",
        "fontconfig",
        "mesa_shader_cache");

    public string CleanerName => "SystemCache";

    public bool IsAvailable(OperatingSystemType operatingSystem) => true;

    public async Task<Result<IReadOnlyList<CleanableItem>, DomainError>> AnalyzeAsync(CancellationToken cancellationToken)
    {
        var cachePath = environment.SystemCachePath();

        if (!fileSystem.DirectoryExists(cachePath) || !fileSystem.IsDirectoryNotEmpty(cachePath))
            return Result<IReadOnlyList<CleanableItem>, DomainError>.Success([]);

        var directoriesResult = await fileSystem.FindDirectoriesAsync(cachePath, "*", cancellationToken);
        var directories = directoriesResult.Recover([]).Value;

        var cachePathString = cachePath.ToString();
        var immediateChildren = from dir in directories
            where dir.DirectoryPath() == cachePathString
            select dir;

        List<CleanableItem> items = [..
            immediateChildren
                .SelectMany(subdirectory => BuildCacheItem(subdirectory).ToEnumerable())];

        return Result<IReadOnlyList<CleanableItem>, DomainError>.Success(items);
    }

    public async Task<Result<CleanupResult, DomainError>> CleanAsync(
        IReadOnlyList<CleanableItem> items, CancellationToken cancellationToken)
    {
        var accumulated = CleanupResult.Empty;
        var ownItems = from item in items
            where item.Reason is not null && OwnReasons.Contains(item.Reason)
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

    private Option<CleanableItem> BuildCacheItem(FilePath subdirectory)
    {
        var dirName = subdirectory.FileName();

        if (SkipDirectories.Contains(dirName))
            return Option<CleanableItem>.None;

        var lastWrite = fileSystem.LastWriteTime(subdirectory);
        if (DateTime.UtcNow - lastWrite <= TimeSpan.FromDays(StaleThresholdDays))
            return Option<CleanableItem>.None;

        return
            from size in fileSystem.Size(subdirectory).ToOption()
            select CleanableItem.CreateSafe(subdirectory, size, CleanupModuleName.System, CacheSubdirectoryReason);
    }
}
