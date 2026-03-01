using DevSweep.Application.Modules;
using DevSweep.Domain.Common;
using DevSweep.Domain.Enums;
using DevSweep.Domain.Errors;

namespace DevSweep.Tests.Application.Modules;

public class ModuleRegistryShould
{
    [Fact]
    public void RegisterModule()
    {
        var registry = new ModuleRegistry();
        var dockerModule = Substitute.For<ICleanupModule>();
        dockerModule.Name.Returns(CleanupModuleName.Docker);

        registry.Register(dockerModule);
        var result = registry.ForName(CleanupModuleName.Docker);
        var registeredModule = result.Value;
        
        result.IsSuccess.Should().BeTrue();
        registeredModule.Should().BeSameAs(dockerModule);
    }

    [Fact]
    public void FailWhenModuleNotRegistered()
    {
        var registry = new ModuleRegistry();
        
        var result = registry.ForName(CleanupModuleName.Docker);

        result.IsFailure.Should().BeTrue();
        result.Error.IsNotFoundError().Should().BeTrue();
    }

    [Fact]
    public void ReturnAllRegisteredModules()
    {
        var registry = new ModuleRegistry();
        var dockerModule = Substitute.For<ICleanupModule>();
        dockerModule.Name.Returns(CleanupModuleName.Docker);
        var homebrewModule = Substitute.For<ICleanupModule>();
        homebrewModule.Name.Returns(CleanupModuleName.Homebrew);

        registry.Register(dockerModule);
        registry.Register(homebrewModule);

        var modules = registry.Modules();

        modules.Should().HaveCount(2);
        modules.Should().Contain(dockerModule);
        modules.Should().Contain(homebrewModule);
    }

    [Fact]
    public void ReturnEmptyListWhenNoModulesRegistered()
    {
        var registry = new ModuleRegistry();

        var modules = registry.Modules();

        modules.Should().BeEmpty();
    }

    [Fact]
    public void OverwriteModuleWithSameName()
    {
        var registry = new ModuleRegistry();
        var originalDockerModule = Substitute.For<ICleanupModule>();
        originalDockerModule.Name.Returns(CleanupModuleName.Docker);
        var updatedDockerModule = Substitute.For<ICleanupModule>();
        updatedDockerModule.Name.Returns(CleanupModuleName.Docker);

        registry.Register(originalDockerModule);
        registry.Register(updatedDockerModule);

        var result = registry.ForName(CleanupModuleName.Docker);
        var currentModule = result.Value;
        
        result.IsSuccess.Should().BeTrue();
        currentModule.Should().BeSameAs(updatedDockerModule);
        registry.Modules().Should().HaveCount(1);
    }

    [Fact]
    public void FailWhenRegisteringNullModule()
    {
        var registry = new ModuleRegistry();

        var result = registry.Register(null);

        result.IsFailure.Should().BeTrue();
        result.Error.IsValidationError().Should().BeTrue();
    }
}
