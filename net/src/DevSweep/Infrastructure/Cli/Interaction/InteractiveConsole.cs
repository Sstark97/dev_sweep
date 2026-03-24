using DevSweep.Application.Ports.Driven;
using Spectre.Console;

namespace DevSweep.Infrastructure.Cli.Interaction;

public sealed class InteractiveConsole(IAnsiConsole console) : IUserInteraction
{
    public Task<bool> ConfirmAsync(string message, bool isDestructive, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromResult(false);

        if (isDestructive)
            console.MarkupLine($"[yellow bold][[WARNING]][/] {Markup.Escape(message)}");

        var prompt = new ConfirmationPrompt(Markup.Escape(message))
        {
            DefaultValue = !isDestructive
        };

        var confirmed = console.Prompt(prompt);
        return Task.FromResult(confirmed);
    }
}
