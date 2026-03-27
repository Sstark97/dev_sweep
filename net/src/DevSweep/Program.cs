using DevSweep.Infrastructure.Cli.Commands;
using DotMake.CommandLine;

namespace DevSweep;

internal sealed class Program
{
    private static async Task<int> Main(string[] args)
    {
        return await Cli.RunAsync<RootCommand>(args);
    }
}
