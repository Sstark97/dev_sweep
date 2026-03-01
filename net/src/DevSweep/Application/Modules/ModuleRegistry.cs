using DevSweep.Domain.Common;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;

namespace DevSweep.Application.Modules;

public sealed class ModuleRegistry
{
    private readonly Dictionary<CleanupModuleName, ICleanupModule> modules = new();

    public Result<Unit, DomainError> Register(ICleanupModule? module)
    {
        if (module is null)
            return Result<Unit, DomainError>.Failure(
                DomainError.Validation("module is required"));

        modules[module.Name] = module;
        return Result<Unit, DomainError>.Success(Unit.Value);
    }

    public Result<ICleanupModule, DomainError> ForName(CleanupModuleName name)
    {
        if (modules.TryGetValue(name, out var module))
            return Result<ICleanupModule, DomainError>.Success(module);

        return Result<ICleanupModule, DomainError>.Failure(
            DomainError.NotFound("CleanupModule", name.ToString()));
    }

    public IReadOnlyList<ICleanupModule> Modules() => [.. modules.Values];
}
