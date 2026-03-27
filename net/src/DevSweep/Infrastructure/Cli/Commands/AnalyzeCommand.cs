using DevSweep.Application.Ports.Driven;
using DevSweep.Application.Ports.Driving;
using DevSweep.Domain.Common;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DotMake.CommandLine;

namespace DevSweep.Infrastructure.Cli.Commands;

[CliCommand(Name = "analyze", Description = "Analyze caches and show what can be cleaned", Parent = typeof(RootCommand))]
public sealed class AnalyzeCommand(
    IAnalyzeUseCase analyzeUseCase,
    IAvailableModulesUseCase availableModulesUseCase,
    IOutputFormatter outputFormatter)
{
    [CliArgument(Description = "Module names to analyze (e.g., jetbrains docker)")]
    public List<string> Modules { get; set; } = [];

    [CliOption(Description = "Analyze all available modules")]
    public bool All { get; set; }

    public async Task<int> RunAsync()
    {
        var modulesResult = ResolveModules();
        if (modulesResult.IsFailure)
        {
            outputFormatter.Error(modulesResult.Error.ToString());
            return 1;
        }

        var result = await analyzeUseCase.Invoke(modulesResult.Value, CancellationToken.None);

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
