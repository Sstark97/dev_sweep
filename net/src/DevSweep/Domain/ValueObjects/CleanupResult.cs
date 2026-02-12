using DevSweep.Domain.Common;
using DevSweep.Domain.Errors;

namespace DevSweep.Domain.ValueObjects;

public readonly record struct CleanupResult
{
    private readonly int filesDeleted;
    private readonly FileSize spaceFreed;
    private readonly IReadOnlyList<string> errors;
    private const int MinimumFilesDeleted = 0;

    private CleanupResult(int filesDeleted, FileSize spaceFreed, IReadOnlyList<string> errors)
    {
        this.filesDeleted = filesDeleted;
        this.spaceFreed = spaceFreed;
        this.errors = errors;
    }

    public static Result<CleanupResult, DomainError> Create(
        int filesDeleted,
        FileSize bytesFreed)
    {
        if (filesDeleted < MinimumFilesDeleted)
            return Result<CleanupResult, DomainError>.Failure(
                DomainError.Validation("Files deleted cannot be negative"));

        return Result<CleanupResult, DomainError>.Success(
            new CleanupResult(filesDeleted, bytesFreed, []));
    }

    public static Result<CleanupResult, DomainError> CreateWithErrors(
        int filesDeleted,
        FileSize bytesFreed,
        IReadOnlyList<string> errors)
    {
        if (filesDeleted < MinimumFilesDeleted)
            return Result<CleanupResult, DomainError>.Failure(
                DomainError.Validation("Files deleted cannot be negative"));

        return Result<CleanupResult, DomainError>.Success(
            new CleanupResult(filesDeleted, bytesFreed, errors));
    }

    public int TotalFilesDeleted() => filesDeleted;
    public FileSize TotalSpaceFreed() => spaceFreed;
    public bool HasErrors() => errors.Count > 0;
    public IReadOnlyList<string> ErrorMessages() => errors;

    public CleanupResult Combine(CleanupResult other) => new(
        filesDeleted + other.filesDeleted,
        spaceFreed.Add(other.spaceFreed),
        [.. errors, .. other.errors]);
}
