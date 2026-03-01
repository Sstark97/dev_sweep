using DevSweep.Domain.Common;
using DevSweep.Domain.Errors;

namespace DevSweep.Application.Ports.Driven;

public interface IProcessManager
{
    bool IsProcessRunning(string processName);
    Task<Result<bool, DomainError>> KillProcessAsync(string processName, CancellationToken cancellationToken);
}
