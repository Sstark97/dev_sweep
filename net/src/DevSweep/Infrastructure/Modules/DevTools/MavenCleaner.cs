using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Common;
using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Infrastructure.Modules.DevTools;

public sealed class MavenCleaner(
    IFileSystem fileSystem,
    IEnvironmentProvider environment) : IDevToolsCleaner
{
    private const string DependencyCacheReason = "devtools:maven-dependency-cache";

    public string CleanerName => "Maven";

    public bool IsAvailable(OperatingSystemType operatingSystem) => true;

    public Task<Result<IReadOnlyList<CleanableItem>, DomainError>> AnalyzeAsync(CancellationToken cancellationToken)
    {
        var repositoryPath = environment.MavenRepositoryPath();

        if (!fileSystem.DirectoryExists(repositoryPath) || !fileSystem.IsDirectoryNotEmpty(repositoryPath))
            return Task.FromResult(Result<IReadOnlyList<CleanableItem>, DomainError>.Success([]));

        var result =
            from size in fileSystem.Size(repositoryPath)
            select (IReadOnlyList<CleanableItem>)[CleanableItem.CreateSafe(repositoryPath, size, CleanupModuleName.DevTools, DependencyCacheReason)];

        return Task.FromResult(result);
    }

    public async Task<Result<CleanupResult, DomainError>> CleanAsync(
        IReadOnlyList<CleanableItem> items, CancellationToken cancellationToken)
    {
        var accumulated = CleanupResult.Empty;
        var ownItems = from item in items where item.Reason == DependencyCacheReason select item;

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
}
