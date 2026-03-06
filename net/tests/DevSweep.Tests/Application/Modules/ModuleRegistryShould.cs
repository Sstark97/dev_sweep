using AwesomeAssertions;
using DevSweep.Application.Modules;
using DevSweep.Domain.Enums;
using NSubstitute;

namespace DevSweep.Tests.Application.Modules;

internal sealed class ModuleRegistryShould
{
    [Test]
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

    [Test]
    public void FailWhenModuleNotRegistered()
    {
        var registry = new ModuleRegistry();
        
        var result = registry.ForName(CleanupModuleName.Docker);

        result.IsFailure.Should().BeTrue();
        result.Error.IsNotFoundError().Should().BeTrue();
    }

    [Test]
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

    [Test]
    public void ReturnEmptyListWhenNoModulesRegistered()
    {
        var registry = new ModuleRegistry();

        var modules = registry.Modules();

        modules.Should().BeEmpty();
    }

    [Test]
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

    [Test]
    public void FailWhenRegisteringNullModule()
    {
        var registry = new ModuleRegistry();

        var result = registry.Register(null);

        result.IsFailure.Should().BeTrue();
        result.Error.IsValidationError().Should().BeTrue();
    }
}
