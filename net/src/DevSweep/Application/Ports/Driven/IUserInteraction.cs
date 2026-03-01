namespace DevSweep.Application.Ports.Driven;

public interface IUserInteraction
{
    Task<bool> ConfirmAsync(string message, bool isDestructive, CancellationToken cancellationToken);
}
