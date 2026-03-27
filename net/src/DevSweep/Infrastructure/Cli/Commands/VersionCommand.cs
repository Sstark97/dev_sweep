using DevSweep.Application.Ports.Driven;
using DotMake.CommandLine;

namespace DevSweep.Infrastructure.Cli.Commands;

[CliCommand(Name = "version", Description = "Display version information", Parent = typeof(RootCommand))]
public sealed class VersionCommand(IOutputFormatter outputFormatter)
{
    public Task<int> RunAsync()
    {
        var version = typeof(VersionCommand).Assembly.GetName().Version?.ToString() ?? "unknown";

        outputFormatter.Info($"DevSweep {version}");
        return Task.FromResult(0);
    }
}
