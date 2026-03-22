using DevSweep.Application.Models;
using DevSweep.Application.Modules;
using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Common;
using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Infrastructure.Modules.Projects;

public sealed class StaleProjectsModule(
    IFileSystem fileSystem,
    IEnvironmentProvider environment,
    IReadOnlyList<IStaleProjectCleaner> cleaners) : ICleanupModule
{
    public CleanupModuleName Name => CleanupModuleName.Projects;
    public string Description => "Clean stale project artifacts from inactive projects (90+ days)";
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
        var analysisTasks = from c in ActiveCleaners()
            select AnalyzeCleanerAsync(c, cancellationToken);
        var analysisResults = await Task.WhenAll(analysisTasks);

        List<CleanableItem> allItems = [.. from result in analysisResults
            where result.IsSuccess
            from item in result.Value
            select item];

        return ModuleAnalysis.Create(CleanupModuleName.Projects, allItems);
    }

    public async Task<Result<CleanupResult, DomainError>> CleanAsync(
        IReadOnlyList<CleanableItem> artifactsToClean, CancellationToken cancellationToken)
    {
        if (artifactsToClean.Count == 0)
            return Result<CleanupResult, DomainError>.Success(CleanupResult.Empty);

        var accumulated = CleanupResult.Empty;
        var projectItems = from item in artifactsToClean
            where item.ModuleType == CleanupModuleName.Projects
            select item;

        foreach (var item in projectItems)
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

    private async Task<Result<IReadOnlyList<CleanableItem>, DomainError>> AnalyzeCleanerAsync(
        IStaleProjectCleaner cleaner, CancellationToken cancellationToken)
    {
        var allItems = new List<CleanableItem>();

        foreach (var artifactDirName in cleaner.ArtifactDirectoryNames)
        {
            var foundDirs = await fileSystem.FindDirectoriesAsync(
                environment.HomePath, artifactDirName, environment.StaleProjectMaxDepth,
                cleaner.DirectoriesToSkipDuringScan, cancellationToken);

            var directories = foundDirs.Recover([]).Value;

            var artifactItemTasks = from artifactPath in directories
                select AnalyzeArtifactDirectoryAsync(artifactPath, cleaner, cancellationToken);

            var artifactResults = await Task.WhenAll(artifactItemTasks);

            allItems.AddRange(from result in artifactResults
                where result.IsSome
                from item in result.ToEnumerable()
                select item);
        }

        return Result<IReadOnlyList<CleanableItem>, DomainError>.Success(allItems);
    }

    private async Task<Option<CleanableItem>> AnalyzeArtifactDirectoryAsync(
        FilePath artifactPath, IStaleProjectCleaner cleaner, CancellationToken cancellationToken)
    {
        var parentPathResult = FilePath.Create(artifactPath.DirectoryPath());
        if (parentPathResult.IsFailure)
            return Option<CleanableItem>.None;

        var projectDir = parentPathResult.Value;

        if (!fileSystem.DirectoryExists(projectDir) || !cleaner.IsProjectDirectory(fileSystem, projectDir))
            return Option<CleanableItem>.None;

        var staleDays = await DaysInactiveAsync(projectDir, cleaner, cancellationToken);
        if (staleDays.IsNone)
            return Option<CleanableItem>.None;

        return fileSystem.Size(artifactPath)
            .ToOption()
            .Map(size => CleanableItem.CreateSafe(
                artifactPath, size, CleanupModuleName.Projects,
                $"projects:{cleaner.CleanerName}-stale-{staleDays.ValueOr(0)}d"));
    }

    private async Task<Option<int>> DaysInactiveAsync(
        FilePath projectDirectory, IStaleProjectCleaner cleaner, CancellationToken cancellationToken)
    {
        var writeTimeResult = await fileSystem.MostRecentWriteTimeAsync(
            projectDirectory, cleaner.DirectoriesToExcludeFromActivityCheck, cancellationToken);

        return writeTimeResult
            .ToOption()
            .Map(writeTime => (int)(DateTime.UtcNow - writeTime).TotalDays)
            .Filter(daysInactive => daysInactive >= environment.StaleProjectDays);
    }

    private IEnumerable<IStaleProjectCleaner> ActiveCleaners() =>
        from cleaner in cleaners
        where cleaner.IsAvailable(environment.CurrentOperatingSystem)
        select cleaner;
}
