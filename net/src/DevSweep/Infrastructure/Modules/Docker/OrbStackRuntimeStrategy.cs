using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Common;
using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Infrastructure.Modules.Docker;

public sealed class OrbStackRuntimeStrategy(
    ICommandRunner commandRunner,
    IFileSystem fileSystem,
    IProcessManager processManager,
    IEnvironmentProvider environment) : IContainerRuntimeStrategy
{
    private const string CacheReason = "docker:orbstack-cache";

    public string RuntimeName => "OrbStack";

    public bool IsAvailable(OperatingSystemType operatingSystem) =>
        operatingSystem == OperatingSystemType.MacOS;

    public Task<Result<IReadOnlyList<CleanableItem>, DomainError>> AnalyzeAsync(CancellationToken cancellationToken)
    {
        var cacheItem = BuildCacheItem();
        return Task.FromResult(
            cacheItem.Match(
                item => Result<IReadOnlyList<CleanableItem>, DomainError>.Success([item]),
                () => Result<IReadOnlyList<CleanableItem>, DomainError>.Success([])));
    }

    public async Task<Result<CleanupResult, DomainError>> CleanAsync(
        IReadOnlyList<CleanableItem> items, CancellationToken cancellationToken)
    {
        var orbItem = items
            .Where(IsOrbStackItem)
            .Select(Option<CleanableItem>.Some)
            .FirstOrDefault(Option<CleanableItem>.None);

        return await orbItem.Match(
            async item =>
            {
                var cacheResult = await CleanCacheAsync(item.Path, cancellationToken);
                return cacheResult.Match(
                    _ => CleanupResult.Create(1, item.Size),
                    error => CleanupResult.CreateWithErrors(0, FileSize.Zero, [$"OrbStack cleanup failed: {error}"]));
            },
            () => Task.FromResult(Result<CleanupResult, DomainError>.Success(CleanupResult.Empty)));
    }

    private Option<CleanableItem> BuildCacheItem()
    {
        if (!OrbStackPresent())
            return Option<CleanableItem>.None;

        return
            from path in FilePath.Create(Path.Combine(environment.HomePath.ToString(), ".orbstack", "cache")).ToOption()
            where fileSystem.DirectoryExists(path) && fileSystem.IsDirectoryNotEmpty(path)
            from size in fileSystem.Size(path).ToOption()
            select CleanableItem.CreateUnsafe(path, size, CleanupModuleName.Docker, CacheReason);
    }

    private async Task<Result<Unit, DomainError>> CleanCacheAsync(
        FilePath cachePath, CancellationToken cancellationToken)
    {
        var ensureStopped = await EnsureStoppedAsync(cancellationToken);
        if (ensureStopped.IsFailure)
            return ensureStopped;

        if (!fileSystem.DirectoryExists(cachePath))
            return Result<Unit, DomainError>.Success(Unit.Value);

        return await fileSystem.DeleteDirectoryAsync(cachePath, cancellationToken);
    }

    private async Task<Result<Unit, DomainError>> EnsureStoppedAsync(CancellationToken cancellationToken)
    {
        if (!processManager.IsProcessRunning("OrbStack"))
            return Result<Unit, DomainError>.Success(Unit.Value);

        var killResult = await processManager.KillProcessAsync("OrbStack", cancellationToken);
        if (killResult.IsFailure)
            return Result<Unit, DomainError>.Failure(
                DomainError.InvalidOperation($"Failed to stop OrbStack: {killResult.Error}"));

        return Result<Unit, DomainError>.Success(Unit.Value);
    }

    private bool OrbStackPresent() =>
        FilePath.Create(Path.Combine(environment.HomePath.ToString(), ".orbstack"))
            .ToOption()
            .Filter(fileSystem.DirectoryExists)
            .IsSome
        || commandRunner.IsCommandAvailable("orbctl");

    private static bool IsOrbStackItem(CleanableItem item) =>
        !item.IsSafeToDelete && item.Reason == CacheReason;
}
