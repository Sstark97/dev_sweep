using DevSweep.Domain.Common;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;

namespace DevSweep.Application.Models;

public readonly record struct ModuleDescriptor
{
    private readonly CleanupModuleName name;
    private readonly string description;
    private readonly bool isDestructive;

    private ModuleDescriptor(CleanupModuleName name, string description, bool isDestructive)
    {
        this.name = name;
        this.description = description;
        this.isDestructive = isDestructive;
    }

    public static Result<ModuleDescriptor, DomainError> Create(CleanupModuleName name, string description, bool isDestructive)
    {
        if (string.IsNullOrWhiteSpace(description))
            return Result<ModuleDescriptor, DomainError>.Failure(
                DomainError.Validation("Description cannot be null or whitespace"));

        return Result<ModuleDescriptor, DomainError>.Success(
            new ModuleDescriptor(name, description, isDestructive));
    }

    public CleanupModuleName Name() => name;
    public string Description() => description;
    public bool IsDestructive() => isDestructive;
}
