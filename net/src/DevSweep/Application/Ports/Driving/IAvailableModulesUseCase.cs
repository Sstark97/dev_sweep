using DevSweep.Application.Models;

namespace DevSweep.Application.Ports.Driving;

public interface IAvailableModulesUseCase
{
    IReadOnlyList<ModuleDescriptor> Invoke();
}
