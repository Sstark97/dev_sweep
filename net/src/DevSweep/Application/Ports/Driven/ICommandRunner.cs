using DevSweep.Application.Models;
using DevSweep.Domain.Common;
using DevSweep.Domain.Errors;

namespace DevSweep.Application.Ports.Driven;

public interface ICommandRunner
{
    bool IsCommandAvailable(string command);
    Task<Result<CommandOutput, DomainError>> RunAsync(
        string command, string arguments, CancellationToken cancellationToken);
}
