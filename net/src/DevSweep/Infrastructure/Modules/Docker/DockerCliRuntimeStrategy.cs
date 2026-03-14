using DevSweep.Application.Models;
using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Common;
using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Infrastructure.Modules.Docker;

public sealed class DockerCliRuntimeStrategy(
    ICommandRunner commandRunner,
    IEnvironmentProvider environment) : IContainerRuntimeStrategy
{
    private const long BytesPerMegabyte = 1024L * 1024L;
    private const long ContainerEstimateMegabytes = 10L;
    private const long DanglingImageEstimateMegabytes = 100L;
    private const long VolumeEstimateMegabytes = 10L;
    private const long BuildCacheEstimateMegabytes = 50L;
    private const long NetworkEstimateMegabytes = 1L;
    private const long AggressiveEstimateMegabytes = 2000L;

    private const string SafePruneReason = "docker:safe";
    private const string AggressivePruneReason = "docker:aggressive";

    public string RuntimeName => "Docker CLI";

    public bool IsAvailable(OperatingSystemType operatingSystem) => true;

    public async Task<Result<IReadOnlyList<CleanableItem>, DomainError>> AnalyzeAsync(CancellationToken cancellationToken)
    {
        if (!commandRunner.IsCommandAvailable("docker"))
            return Result<IReadOnlyList<CleanableItem>, DomainError>.Success([]);

        var daemonResult = await commandRunner.RunAsync("docker", "info", cancellationToken);
        if (!daemonResult.Match(output => output.IsSuccessful(), _ => false))
            return Result<IReadOnlyList<CleanableItem>, DomainError>.Success([]);

        var configPath = environment.DockerConfigPath();
        return await BuildCandidatesAsync(configPath, cancellationToken);
    }

    public async Task<Result<CleanupResult, DomainError>> CleanAsync(
        IReadOnlyList<CleanableItem> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0 || !commandRunner.IsCommandAvailable("docker"))
            return Result<CleanupResult, DomainError>.Success(CleanupResult.Empty);

        var safeItems = items.Where(item => item.IsSafeToDelete).ToList();
        var aggressiveItems = items.Where(IsAggressiveItem).ToList();

        var safeResult = await PruneAsync("system prune -f --volumes", "Docker safe prune failed", safeItems, cancellationToken);
        var aggressiveResult = await PruneAsync("system prune -af --volumes", "Docker aggressive prune failed", aggressiveItems, cancellationToken);

        return
            from safe in safeResult
            from aggressive in aggressiveResult
            select safe.Combine(aggressive);
    }

    private async Task<Result<IReadOnlyList<CleanableItem>, DomainError>> BuildCandidatesAsync(
        FilePath configPath, CancellationToken cancellationToken)
    {
        var safeEstimate = await EstimateSafePruneSizeAsync(cancellationToken);

        List<Result<CleanableItem, DomainError>> candidates = [AggressiveItem(configPath)];
        if (safeEstimate > 0)
            candidates.Add(SafeItem(configPath, safeEstimate));

        return candidates.Collect(r => r);
    }

    private async Task<Result<CleanupResult, DomainError>> PruneAsync(
        string args, string errorLabel, List<CleanableItem> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0)
            return Result<CleanupResult, DomainError>.Success(CleanupResult.Empty);

        var commandResult = await commandRunner.RunAsync("docker", args, cancellationToken);

        if (!commandResult.Match(output => output.IsSuccessful(), _ => false))
            return CleanupResult.CreateWithErrors(0, FileSize.Zero,
                [$"{errorLabel}: {CommandOutput.ErrorMessage(commandResult)}"]);

        var freed = items.Aggregate(FileSize.Zero, (acc, item) => acc.Add(item.Size));
        return CleanupResult.Create(items.Count, freed);
    }

    private async Task<long> EstimateSafePruneSizeAsync(CancellationToken cancellationToken)
    {
        var stoppedTask = commandRunner.RunAsync("docker", "ps -aq --filter status=exited", cancellationToken);
        var danglingTask = commandRunner.RunAsync("docker", "images -f dangling=true -q", cancellationToken);
        var volumesTask = commandRunner.RunAsync("docker", "volume ls -q", cancellationToken);
        var networksTask = commandRunner.RunAsync("docker", "network ls --format {{.Name}}", cancellationToken);

        var results = await Task.WhenAll(stoppedTask, danglingTask, volumesTask, networksTask);

        return
            EstimateFromOutput(results[0], CountLines, ContainerEstimateMegabytes) +
            EstimateFromOutput(results[1], CountLines, DanglingImageEstimateMegabytes) +
            EstimateFromOutput(results[2], CountLines, VolumeEstimateMegabytes) +
            EstimateFromOutput(results[3], CountCustomNetworks, NetworkEstimateMegabytes) +
            BuildCacheEstimateMegabytes * BytesPerMegabyte;
    }

    private static long EstimateFromOutput(
        Result<CommandOutput, DomainError> result,
        Func<string, int> counter,
        long megabytesPerItem) =>
        result.ToOption()
            .Filter(output => output.IsSuccessful())
            .Map(output => counter(output.StandardOutput()) * megabytesPerItem * BytesPerMegabyte)
            .ValueOr(0L);

    private static bool IsAggressiveItem(CleanableItem item) =>
        !item.IsSafeToDelete && item.Reason == AggressivePruneReason;

    private static Result<CleanableItem, DomainError> SafeItem(FilePath path, long bytes) =>
        from size in FileSize.Create(bytes)
        select CleanableItem.CreateSafe(path, size, CleanupModuleName.Docker, SafePruneReason);

    private static Result<CleanableItem, DomainError> AggressiveItem(FilePath path) =>
        from size in FileSize.FromMegabytes(AggressiveEstimateMegabytes)
        select CleanableItem.CreateUnsafe(path, size, CleanupModuleName.Docker, AggressivePruneReason);

    private static int CountLines(string output) =>
        output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;

    private static int CountCustomNetworks(string output) =>
        output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Count(name => name is not ("bridge" or "host" or "none"));
}
