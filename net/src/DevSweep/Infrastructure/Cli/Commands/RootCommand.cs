using DevSweep.Application.Ports.Driven;
using DotMake.CommandLine;

namespace DevSweep.Infrastructure.Cli.Commands;

[CliCommand(Description = "DevSweep - Developer cache cleaner")]
public sealed class RootCommand(IOutputFormatter outputFormatter)
{
    [CliOption(Description = "Preview without deleting", Name = "--dry-run", Aliases = ["-d"])]
    public bool DryRun { get; set; }

    [CliOption(Description = "Skip confirmations", Name = "--force", Aliases = ["-f", "-y"])]
    public bool Force { get; set; }

    [CliOption(Description = "Detailed output", Name = "--verbose")]
    public bool Verbose { get; set; }

    [CliOption(Description = "Output format: rich, plain, json", Name = "--output")]
    public string Output { get; set; } = "rich";

    public Task<int> RunAsync()
    {
        outputFormatter.Info("Run 'devsweep --help' for usage information");
        return Task.FromResult(0);
    }
}
