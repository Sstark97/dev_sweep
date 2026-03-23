using DevSweep.Domain.Common;
using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Infrastructure.Modules.System;

public interface ISystemCleaner
{
    string CleanerName { get; }
    bool IsAvailable(OperatingSystemType operatingSystem);
    Task<Result<IReadOnlyList<CleanableItem>, DomainError>> AnalyzeAsync(CancellationToken cancellationToken);
    Task<Result<CleanupResult, DomainError>> CleanAsync(IReadOnlyList<CleanableItem> items, CancellationToken cancellationToken);
}
