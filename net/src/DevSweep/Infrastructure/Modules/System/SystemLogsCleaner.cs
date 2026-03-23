using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Common;
using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Infrastructure.Modules.System;

public sealed class SystemLogsCleaner(
    IFileSystem fileSystem,
    IEnvironmentProvider environment) : ISystemCleaner
{
    private const int StaleThresholdDays = 30;
    private const string UserLogsReason = "system:user-logs";
    private const string SystemLogsReason = "system:system-logs";

    private static readonly HashSet<string> OwnReasons = [UserLogsReason, SystemLogsReason];

    public string CleanerName => "SystemLogs";

    public bool IsAvailable(OperatingSystemType operatingSystem) => true;

    public async Task<Result<IReadOnlyList<CleanableItem>, DomainError>> AnalyzeAsync(CancellationToken cancellationToken)
    {
        var logsPath = environment.SystemLogsPath();

        if (!fileSystem.DirectoryExists(logsPath) || !fileSystem.IsDirectoryNotEmpty(logsPath))
            return Result<IReadOnlyList<CleanableItem>, DomainError>.Success([]);

        var directoriesResult = await fileSystem.FindDirectoriesAsync(logsPath, "*", cancellationToken);
        var directories = directoriesResult.Match(dirs => dirs, _ => []);

        var logsPathString = logsPath.ToString();
        var immediateChildren = from dir in directories
            where dir.DirectoryPath() == logsPathString
            select dir;

        var isLinux = environment.CurrentOperatingSystem == OperatingSystemType.Linux;

        List<CleanableItem> items = [..
            immediateChildren
                .SelectMany(subdirectory => BuildLogsItem(subdirectory, isLinux).ToEnumerable())];

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

    private Option<CleanableItem> BuildLogsItem(FilePath subdirectory, bool isLinux)
    {
        var lastWrite = fileSystem.LastWriteTime(subdirectory);
        if (DateTime.UtcNow - lastWrite <= TimeSpan.FromDays(StaleThresholdDays))
            return Option<CleanableItem>.None;

        var reason = isLinux ? SystemLogsReason : UserLogsReason;

        return isLinux
            ? from size in fileSystem.Size(subdirectory).ToOption()
              select CleanableItem.CreateUnsafe(subdirectory, size, CleanupModuleName.System, reason)
            : from size in fileSystem.Size(subdirectory).ToOption()
              select CleanableItem.CreateSafe(subdirectory, size, CleanupModuleName.System, reason);
    }
}
