using DevSweep.Domain.Common;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Domain.Entities;

public record CleanupSummary(
    CleanupModuleName Module,
    int TotalItemsScanned,
    int SafeItemsFound,
    CleanupResult Result)
{
    public static Result<CleanupSummary, DomainError> Create(
        CleanupModuleName module,
        IReadOnlyList<CleanableItem> items,
        CleanupResult result)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
            return Result<CleanupSummary, DomainError>.Failure(
                DomainError.InvalidOperation("Cannot create summary with no items"));

        var safeCount = items.Count(item => item.IsSafeToDelete);

        return Result<CleanupSummary, DomainError>.Success(
            new CleanupSummary(module, items.Count, safeCount, result));
    }
}
