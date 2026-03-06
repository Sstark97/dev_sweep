using DevSweep.Application.Models;
using DevSweep.Application.Modules;
using DevSweep.Application.Ports.Driven;
using DevSweep.Application.Ports.Driving;

namespace DevSweep.Application.UseCases;

public sealed class AvailableModulesUseCase(
    ModuleRegistry registry, 
    IEnvironmentProvider environmentProvider
) : IAvailableModulesUseCase
{
    public IReadOnlyList<ModuleDescriptor> Invoke()
    {
        var currentOs = environmentProvider.CurrentOperatingSystem;

        return [.. from module in registry.Modules()
            where module.IsAvailableOnPlatform(currentOs)
            let descriptor = ModuleDescriptor.Create(module.Name, module.Description, module.IsDestructive)
            where descriptor.IsSuccess
            select descriptor.Value];
    }
}
