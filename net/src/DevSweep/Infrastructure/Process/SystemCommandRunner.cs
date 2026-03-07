using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DevSweep.Application.Models;
using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Common;
using DevSweep.Domain.Errors;
using SystemProcess = System.Diagnostics.Process;

namespace DevSweep.Infrastructure.Process;

public sealed class SystemCommandRunner : ICommandRunner
{
    public bool IsCommandAvailable(string command)
    {
        try
        {
            var lookupCommand = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "where"
                : "which";

            var (exitCode, _, _) = ExecuteProcess(lookupCommand, command);

            return exitCode == 0;
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
        {
            return false;
        }
    }

    public Task<Result<CommandOutput, DomainError>> RunAsync(
        string command, string arguments, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromResult(Result<CommandOutput, DomainError>.Failure(
                DomainError.InvalidOperation("Operation was cancelled")));

        return Task.Run(() =>
        {
            try
            {
                var (exitCode, standardOutput, standardError) = ExecuteProcess(command, arguments);

                return CommandOutput.Create(exitCode, standardOutput, standardError);
            }
            catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
            {
                return CommandOutput.CreateFailed(exception.Message);
            }
        }, cancellationToken);
    }

    private static (int exitCode, string standardOutput, string standardError) ExecuteProcess(
        string fileName, string arguments)
    {
        var process = new SystemProcess
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        try
        {
            process.Start();

            var standardOutput = process.StandardOutput.ReadToEnd();
            var standardError = process.StandardError.ReadToEnd();

            process.WaitForExit();

            return (process.ExitCode, standardOutput, standardError);
        }
        finally
        {
            process.Dispose();
        }
    }
}
