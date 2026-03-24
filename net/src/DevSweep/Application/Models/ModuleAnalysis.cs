using DevSweep.Domain.Common;
using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Application.Models;

public sealed record ModuleAnalysis
{
    private readonly IReadOnlyList<CleanableItem> items;

    private ModuleAnalysis(CleanupModuleName module, IReadOnlyList<CleanableItem> items)
    {
        Module = module;
        this.items = items;
    }

    public static Result<ModuleAnalysis, DomainError> Create(CleanupModuleName module, IReadOnlyList<CleanableItem>? items)
    {
        if (items is null)
            return Result<ModuleAnalysis, DomainError>.Failure(
                DomainError.Validation("items is required"));

        return Result<ModuleAnalysis, DomainError>.Success(
            new ModuleAnalysis(module, items));
    }

    public static ModuleAnalysis CreateEmpty(CleanupModuleName module) =>
        new(module, []);

    public CleanupModuleName Module { get; }

    public IReadOnlyList<CleanableItem> Items() => items;

    public FileSize TotalSize() =>
        items.Aggregate(FileSize.Zero, (acc, item) => acc.Add(item.Size));

    public int SafeItemCount() => items.Count(item => item.IsSafeToDelete);

    public int UnsafeItemCount() => items.Count(item => !item.IsSafeToDelete);

    public int ItemCount() => items.Count;

    public bool IsEmpty() => items.Count == 0;
}
