using DevSweep.Domain.Common;
using DevSweep.Domain.Entities;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Application.Models;

public sealed record ModuleAnalysis
{
    private ModuleAnalysis(CleanupModuleName module, IReadOnlyList<CleanableItem> items)
    {
        Module = module;
        Items = items;
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
    private IReadOnlyList<CleanableItem> Items { get; }

    public FileSize TotalSize()
    {
        var zero = FileSize.Create(0).Value;
        return Items.Aggregate(zero, (acc, item) => acc.Add(item.Size));
    }

    public int SafeItemCount() => Items.Count(item => item.IsSafeToDelete);

    public int UnsafeItemCount() => Items.Count(item => !item.IsSafeToDelete);

    public int ItemCount() => Items.Count;

    public bool IsEmpty() => Items.Count == 0;
}
