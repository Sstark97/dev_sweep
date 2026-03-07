using System.ComponentModel;
using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Common;
using DevSweep.Domain.Errors;
using SystemProcess = System.Diagnostics.Process;

namespace DevSweep.Infrastructure.Process;

public sealed class SystemProcessManager : IProcessManager
{
    public bool IsProcessRunning(string processName)
    {
        try
        {
            var processes = SystemProcess.GetProcessesByName(processName);
            var isRunning = processes.Length > 0;

            foreach (var process in processes)
                process.Dispose();

            return isRunning;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public Task<Result<bool, DomainError>> KillProcessAsync(
        string processName, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromResult(Result<bool, DomainError>.Failure(
                DomainError.InvalidOperation("Operation was cancelled")));

        return Task.Run(() =>
        {
            try
            {
                var processes = SystemProcess.GetProcessesByName(processName);

                if (processes.Length == 0)
                    return Result<bool, DomainError>.Success(false);

                foreach (var process in processes)
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit();
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }

                return Result<bool, DomainError>.Success(true);
            }
            catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
            {
                return Result<bool, DomainError>.Failure(
                    DomainError.InvalidOperation(exception.Message));
            }
        }, cancellationToken);
    }
}
