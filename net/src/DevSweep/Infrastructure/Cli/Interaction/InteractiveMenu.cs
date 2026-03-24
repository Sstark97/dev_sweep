using DevSweep.Application.Models;
using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Enums;
using Spectre.Console;

namespace DevSweep.Infrastructure.Cli.Interaction;

public sealed class InteractiveMenu(IAnsiConsole console) : IModuleSelector
{
    public Task<IReadOnlyList<CleanupModuleName>> SelectModulesAsync(
        IReadOnlyList<ModuleDescriptor> available,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromResult<IReadOnlyList<CleanupModuleName>>([]);

        var prompt = new MultiSelectionPrompt<string>()
            .Title("Select modules to clean:")
            .PageSize(10)
            .NotRequired();

        foreach (var descriptor in available)
        {
            var label = $"{descriptor.Name()} — {descriptor.Description()}";
            prompt.AddChoice(label);
            prompt.Select(label);
        }

        var selected = console.Prompt(prompt);

        IReadOnlyList<CleanupModuleName> selectedNames = [.. from descriptor in available
            let label = $"{descriptor.Name()} — {descriptor.Description()}"
            where selected.Contains(label)
            select descriptor.Name()];

        return Task.FromResult(selectedNames);
    }
}
