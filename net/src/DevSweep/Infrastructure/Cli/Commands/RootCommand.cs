using DevSweep.Application.Models;
using DevSweep.Application.Ports.Driven;
using DevSweep.Application.Ports.Driving;
using DevSweep.Domain.Enums;
using DotMake.CommandLine;

namespace DevSweep.Infrastructure.Cli.Commands;

[CliCommand(Description = "DevSweep - Developer cache cleaner")]
public sealed class RootCommand(
    IOutputFormatter outputFormatter,
    IAvailableModulesUseCase availableModulesUseCase,
    IModuleSelector moduleSelector,
    IAnalyzeUseCase analyzeUseCase,
    ICleanupUseCase cleanupUseCase)
{
    [CliOption(Description = "Preview without deleting", Name = "--dry-run", Aliases = ["-d"])]
    public bool DryRun { get; set; }

    [CliOption(Description = "Skip confirmations", Name = "--force", Aliases = ["-f", "-y"])]
    public bool Force { get; set; }

    [CliOption(Description = "Detailed output", Name = "--verbose")]
    public bool Verbose { get; set; }

    [CliOption(Description = "Output format: rich, plain, json", Name = "--output")]
    public string Output { get; set; } = "rich";

    public async Task<int> RunAsync()
    {
        var version = typeof(RootCommand).Assembly.GetName().Version?.ToString() ?? "unknown";
        outputFormatter.DisplayBanner(version);

        var available = availableModulesUseCase.Invoke();

        if (available.Count == 0)
        {
            outputFormatter.Info("No modules available on this platform.");
            return 0;
        }

        var selectedModules = await SelectModulesAsync(available);

        if (selectedModules.Count == 0)
            return 0;

        var analysisExitCode = await AnalyzeSelectedModulesAsync(selectedModules);
        if (analysisExitCode != 0)
            return analysisExitCode;

        if (DryRun)
            return 0;

        return await CleanSelectedModulesAsync(selectedModules);
    }

    private async Task<IReadOnlyList<CleanupModuleName>> SelectModulesAsync(
        IReadOnlyList<ModuleDescriptor> available) =>
        await moduleSelector.SelectModulesAsync(available, CancellationToken.None);

    private async Task<int> AnalyzeSelectedModulesAsync(IReadOnlyList<CleanupModuleName> modules)
    {
        var result = await analyzeUseCase.Invoke(modules, CancellationToken.None);

        if (result.IsFailure)
        {
            outputFormatter.Error(result.Error.ToString());
            return 1;
        }

        return 0;
    }

    private async Task<int> CleanSelectedModulesAsync(IReadOnlyList<CleanupModuleName> modules)
    {
        var result = await cleanupUseCase.Invoke(modules, CancellationToken.None);

        if (result.IsFailure)
        {
            outputFormatter.Error(result.Error.ToString());
            return 1;
        }

        return 0;
    }
}
