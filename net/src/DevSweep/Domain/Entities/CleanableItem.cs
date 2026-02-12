using DevSweep.Domain.Common;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Domain.Entities;

public record CleanableItem
{
    private CleanableItem(
        FilePath path,
        FileSize size,
        CleanupModuleName moduleType,
        bool isSafeToDelete,
        string? reason)
    {
        Path = path;
        Size = size;
        ModuleType = moduleType;
        IsSafeToDelete = isSafeToDelete;
        Reason = reason;
    }

    public static CleanableItem CreateSafe(
        FilePath path,
        FileSize size,
        CleanupModuleName moduleType,
        string reason) =>
        new(path, size, moduleType, true, reason);

    public static CleanableItem CreateUnsafe(
        FilePath path,
        FileSize size,
        CleanupModuleName moduleType,
        string reason) =>
        new(path, size, moduleType, false, reason);

    public FilePath Path { get; }

    public FileSize Size { get; }

    public CleanupModuleName ModuleType { get; }

    public bool IsSafeToDelete { get; }

    public string? Reason { get; }

    public Result<CleanableItem, DomainError> MarkForDeletion()
    {
        if (!IsSafeToDelete)
            return Result<CleanableItem, DomainError>.Failure(
                DomainError.InvalidOperation("Cannot mark unsafe item for deletion"));

        return Result<CleanableItem, DomainError>.Success(this);
    }

    public Result<CleanableItem, DomainError> MarkAsUnsafe(string newReason)
    {
        if (!IsSafeToDelete)
            return Result<CleanableItem, DomainError>.Failure(
                DomainError.InvalidOperation("Item is already marked as unsafe"));

        return Result<CleanableItem, DomainError>.Success(
            new CleanableItem(Path, Size, ModuleType, false, newReason));
    }

    public Result<CleanableItem, DomainError> MarkAsSafe(string newReason)
    {
        if (IsSafeToDelete)
            return Result<CleanableItem, DomainError>.Failure(
                DomainError.InvalidOperation("Item is already marked as safe"));

        return Result<CleanableItem, DomainError>.Success(
            new CleanableItem(Path, Size, ModuleType, true, newReason));
    }
}
