using DevSweep.Application.Models;
using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Enums;

namespace DevSweep.Infrastructure.Cli.Interaction;

public sealed class SelectAllModules : IModuleSelector
{
    public Task<IReadOnlyList<CleanupModuleName>> SelectModulesAsync(
        IReadOnlyList<ModuleDescriptor> available,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<CleanupModuleName>>(
            [.. from descriptor in available select descriptor.Name()]);
}
