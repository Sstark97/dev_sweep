namespace DevSweep.Tests.Infrastructure.Cli.E2E;

internal sealed record CliProcessResult(int ExitCode, string StandardOutput, string StandardError);

internal static class CliProcessRunner
{
    private static readonly string ProjectPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DevSweep", "DevSweep.csproj"));

    public static async Task<CliProcessResult> RunAsync(string arguments, CancellationToken cancellationToken)
    {
        var binaryPath = System.Environment.GetEnvironmentVariable("DEVSWEEP_BINARY_PATH");

        var startInfo = binaryPath is not null
            ? new System.Diagnostics.ProcessStartInfo
            {
                FileName = binaryPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
            : new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project {ProjectPath} --no-build -- {arguments}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

        using var process = new System.Diagnostics.Process { StartInfo = startInfo };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        try
        {
            await Task.WhenAll(
                stdoutTask,
                stderrTask,
                process.WaitForExitAsync(cancellationToken));
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return new CliProcessResult(process.ExitCode, stdout, stderr);
    }
}
