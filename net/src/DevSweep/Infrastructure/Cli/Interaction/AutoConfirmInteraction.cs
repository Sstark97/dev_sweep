using DevSweep.Application.Ports.Driven;

namespace DevSweep.Infrastructure.Cli.Interaction;

public sealed class AutoConfirmInteraction : IUserInteraction
{
    public Task<bool> ConfirmAsync(string message, bool isDestructive, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromResult(false);

        return Task.FromResult(true);
    }
}
