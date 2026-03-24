using AwesomeAssertions;
using DevSweep.Infrastructure.Cli.Interaction;

namespace DevSweep.Tests.Infrastructure.Cli.Interaction;

internal sealed class AutoConfirmInteractionShould
{
    [Test]
    public async Task ConfirmNonDestructiveOperation()
    {
        var interaction = new AutoConfirmInteraction();

        var confirmed = await interaction.ConfirmAsync("Proceed?", isDestructive: false, CancellationToken.None);

        confirmed.Should().BeTrue();
    }

    [Test]
    public async Task ConfirmDestructiveOperation()
    {
        var interaction = new AutoConfirmInteraction();

        var confirmed = await interaction.ConfirmAsync("Delete everything?", isDestructive: true, CancellationToken.None);

        confirmed.Should().BeTrue();
    }

    [Test]
    public async Task NotConfirmWhenCancellationRequested()
    {
        var interaction = new AutoConfirmInteraction();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var confirmed = await interaction.ConfirmAsync("Proceed?", isDestructive: false, cts.Token);

        confirmed.Should().BeFalse();
    }
}
