using DevSweep.Application.Models;

namespace DevSweep.Application.Ports.Driving;

public interface IModuleDiscoveryUseCase
{
    IReadOnlyList<ModuleDescriptor> AvailableModules();
}
