using AwesomeAssertions;
using DevSweep.Infrastructure.Cli.Interaction;
using Spectre.Console.Testing;

namespace DevSweep.Tests.Infrastructure.Cli.Interaction;

internal sealed class InteractiveConsoleShould
{
    [Test]
    public async Task NotConfirmWhenCancellationRequested()
    {
        using var console = new TestConsole();
        var interaction = new InteractiveConsole(console);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var confirmed = await interaction.ConfirmAsync("Proceed?", isDestructive: false, cts.Token);

        confirmed.Should().BeFalse();
    }

    [Test]
    public async Task WriteDestructiveWarningWhenOperationIsDestructive()
    {
        using var console = new TestConsole();
        console.Interactive();
        console.Input.PushTextWithEnter("y");
        var interaction = new InteractiveConsole(console);

        await interaction.ConfirmAsync("Delete cache?", isDestructive: true, CancellationToken.None);

        var output = console.Output;
        output.Should().Contain("WARNING");
    }

    [Test]
    public async Task ConfirmWhenOperationIsNotDestructive()
    {
        using var console = new TestConsole();
        console.Interactive();
        console.Input.PushTextWithEnter("y");
        var interaction = new InteractiveConsole(console);

        var confirmed = await interaction.ConfirmAsync("Proceed?", isDestructive: false, CancellationToken.None);

        confirmed.Should().BeTrue();
    }

    [Test]
    public async Task NotWriteWarningWhenOperationIsNotDestructive()
    {
        using var console = new TestConsole();
        console.Interactive();
        console.Input.PushTextWithEnter("y");
        var interaction = new InteractiveConsole(console);

        await interaction.ConfirmAsync("Proceed?", isDestructive: false, CancellationToken.None);

        var output = console.Output;
        output.Should().NotContain("WARNING");
    }
}
