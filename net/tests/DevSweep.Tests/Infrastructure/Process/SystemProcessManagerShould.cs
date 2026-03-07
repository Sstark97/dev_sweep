using System.Diagnostics;
using AwesomeAssertions;
using DevSweep.Infrastructure.Process;
using SystemProcess = System.Diagnostics.Process;

namespace DevSweep.Tests.Infrastructure.Process;

internal sealed class SystemProcessManagerShould : IDisposable
{
    private readonly SystemProcessManager processManager = new();
    private readonly List<IDisposable> disposables = [];

    [Test]
    public void DetectCurrentlyRunningProcess()
    {
        var currentProcess = SystemProcess.GetCurrentProcess().ProcessName;

        var isRunning = processManager.IsProcessRunning(currentProcess);

        isRunning.Should().BeTrue();
    }

    [Test]
    public void NotDetectNonExistentProcess()
    {
        var isRunning = processManager.IsProcessRunning("devsweep-nonexistent-process-xyz");

        isRunning.Should().BeFalse();
    }

    [Test]
    public async Task NotKillNonExistentProcess()
    {
        var result = await processManager.KillProcessAsync(
            "devsweep-nonexistent-process-xyz", CancellationToken.None);

        var killed = result.Value;
        result.IsSuccess.Should().BeTrue();
        killed.Should().BeFalse();
    }

    [Test]
    public async Task FailWhenCancellationIsRequested()
    {
        var cancellationSource = Track(new CancellationTokenSource());
        await cancellationSource.CancelAsync();

        var result = await processManager.KillProcessAsync(
            "any-process", cancellationSource.Token);

        result.IsFailure.Should().BeTrue();
        result.Error.IsInvalidOperationError().Should().BeTrue();
    }

    [Test]
    public async Task KillSpawnedProcessSuccessfully()
    {
        var spawnedProcess = Track(SpawnLongRunningProcess());

        var result = await processManager.KillProcessAsync(
            spawnedProcess.ProcessName, CancellationToken.None);

        var killed = result.Value;
        result.IsSuccess.Should().BeTrue();
        killed.Should().BeTrue();
    }

    private static SystemProcess SpawnLongRunningProcess()
    {
        var process = new SystemProcess
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ping",
                Arguments = OperatingSystem.IsWindows() ? "-n 9999 127.0.0.1" : "-c 9999 127.0.0.1",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            }
        };

        process.Start();
        return process;
    }

    private T Track<T>(T disposable) where T : IDisposable
    {
        disposables.Add(disposable);
        return disposable;
    }

    public void Dispose()
    {
        foreach (var disposable in disposables)
            disposable.Dispose();
    }
}
