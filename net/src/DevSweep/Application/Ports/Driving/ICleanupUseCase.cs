using DevSweep.Domain.Common;
using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;

namespace DevSweep.Application.Ports.Driving;

public interface ICleanupUseCase
{
    Task<Result<IReadOnlyList<CleanupSummary>, DomainError>> ExecuteAsync(
        IReadOnlyList<CleanupModuleName> modules,
        CancellationToken cancellationToken);
}
