using DevSweep.Application.Ports.Driven;
using DevSweep.Application.Ports.Driving;
using DevSweep.Domain.Common;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DotMake.CommandLine;

namespace DevSweep.Infrastructure.Cli.Commands;

[CliCommand(Name = "clean", Description = "Clean developer caches", Parent = typeof(RootCommand))]
public sealed class CleanCommand(
    ICleanupUseCase cleanupUseCase,
    IAvailableModulesUseCase availableModulesUseCase,
    IOutputFormatter outputFormatter)
{
    [CliArgument(Description = "Module names to clean (e.g., jetbrains docker)")]
    public List<string> Modules { get; set; } = [];

    [CliOption(Description = "Clean all available modules")]
    public bool All { get; set; }

    [CliOption(Description = "Nuclear cleanup mode (requires devtools module)")]
    public bool Nuclear { get; set; }

    public async Task<int> RunAsync()
    {
        var modulesResult = ResolveModules();
        if (modulesResult.IsFailure)
        {
            outputFormatter.Error(modulesResult.Error.ToString());
            return 1;
        }

        var moduleNames = modulesResult.Value;

        if (Nuclear && !moduleNames.Contains(CleanupModuleName.DevTools))
        {
            outputFormatter.Error("Nuclear mode requires the devtools module");
            return 1;
        }

        var result = await cleanupUseCase.Invoke(moduleNames, CancellationToken.None);

        if (result.IsFailure)
        {
            outputFormatter.Error(result.Error.ToString());
            return 1;
        }

        return 0;
    }

    private Result<IReadOnlyList<CleanupModuleName>, DomainError> ResolveModules() =>
        ModuleNameParser.Resolve(Modules, All, availableModulesUseCase);
}
