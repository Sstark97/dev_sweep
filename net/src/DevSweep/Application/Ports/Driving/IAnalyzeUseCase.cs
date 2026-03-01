using DevSweep.Application.Models;
using DevSweep.Domain.Common;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;

namespace DevSweep.Application.Ports.Driving;

public interface IAnalyzeUseCase
{
    Task<Result<AnalysisReport, DomainError>> ExecuteAsync(
        IReadOnlyList<CleanupModuleName> modules,
        CancellationToken cancellationToken);
}
