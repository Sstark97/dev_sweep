using DevSweep.Domain.Enums;
using DevSweep.Infrastructure.Cli.Commands;
using DevSweep.Infrastructure.Cli.Composition;
using DotMake.CommandLine;

namespace DevSweep;

internal sealed class Program
{
    private static async Task<int> Main(string[] args)
    {
        var (outputStrategy, autoConfirm) = ParseGlobalOptions(args);

        Cli.Ext.ConfigureServices(services =>
        {
            services.AddDevSweepServices(outputStrategy, autoConfirm);
        });

        return await Cli.RunAsync<RootCommand>(args);
    }

    private static (OutputStrategy outputStrategy, bool autoConfirm) ParseGlobalOptions(string[] args)
    {
        var outputStrategy = OutputStrategy.Rich;
        var autoConfirm = false;

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--output" && i + 1 < args.Length)
            {
                outputStrategy = args[i + 1].ToUpperInvariant() switch
                {
                    "PLAIN" => OutputStrategy.Plain,
                    "JSON" => OutputStrategy.Json,
                    _ => OutputStrategy.Rich
                };
            }
            else if (args[i] is "--force" or "-f" or "-y")
            {
                autoConfirm = true;
            }
        }

        return (outputStrategy, autoConfirm);
    }
}
