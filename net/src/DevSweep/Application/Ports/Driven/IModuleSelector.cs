using DevSweep.Application.Models;
using DevSweep.Domain.Enums;

namespace DevSweep.Application.Ports.Driven;

public interface IModuleSelector
{
    Task<IReadOnlyList<CleanupModuleName>> SelectModulesAsync(
        IReadOnlyList<ModuleDescriptor> available,
        CancellationToken cancellationToken);
}
