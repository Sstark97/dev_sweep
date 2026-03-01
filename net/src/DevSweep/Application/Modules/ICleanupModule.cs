using DevSweep.Application.Models;
using DevSweep.Domain.Common;
using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Application.Modules;

public interface ICleanupModule
{
    CleanupModuleName Name { get; }
    string Description { get; }
    bool IsDestructive { get; }
    bool IsAvailableOnPlatform(OperatingSystemType operatingSystem);
    Task<Result<ModuleAnalysis, DomainError>> AnalyzeAsync(
        CleanupContext context, CancellationToken cancellationToken);
    Task<Result<CleanupResult, DomainError>> CleanAsync(
        CleanupContext context, IReadOnlyList<CleanableItem> items, CancellationToken cancellationToken);
}
