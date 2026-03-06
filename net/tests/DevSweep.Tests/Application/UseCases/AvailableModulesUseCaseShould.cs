using DevSweep.Application.Modules;
using DevSweep.Application.Ports.Driven;
using DevSweep.Application.UseCases;
using DevSweep.Domain.Enums;
using DevSweep.Tests.Application.Builders;

namespace DevSweep.Tests.Application.UseCases;

public class AvailableModulesUseCaseShould
{
    private readonly ModuleRegistry registry = new();
    private readonly IEnvironmentProvider environmentProvider = Substitute.For<IEnvironmentProvider>();

    [Fact]
    public void ReturnEmptyListWhenNoModulesRegistered()
    {
        environmentProvider.CurrentOperatingSystem.Returns(OperatingSystemType.MacOS);
        var useCase = new AvailableModulesUseCase(registry, environmentProvider);

        var descriptors = useCase.Invoke();

        descriptors.Should().BeEmpty();
    }

    [Fact]
    public void ReturnAllModulesAvailableOnCurrentPlatform()
    {
        environmentProvider.CurrentOperatingSystem.Returns(OperatingSystemType.MacOS);

        var dockerModule = Substitute.For<ICleanupModule>();
        new CleanupModuleBuilder(dockerModule)
            .ForModule(CleanupModuleName.Docker)
            .WithDescription("Docker cache cleanup")
            .AvailableOn(OperatingSystemType.MacOS)
            .Configure();

        var homebrewModule = Substitute.For<ICleanupModule>();
        new CleanupModuleBuilder(homebrewModule)
            .ForModule(CleanupModuleName.Homebrew)
            .WithDescription("Homebrew cache cleanup")
            .AvailableOn(OperatingSystemType.MacOS)
            .Configure();

        registry.Register(dockerModule);
        registry.Register(homebrewModule);
        var useCase = new AvailableModulesUseCase(registry, environmentProvider);

        var descriptors = useCase.Invoke();

        descriptors.Should().HaveCount(2);
    }

    [Fact]
    public void ExcludeModulesNotAvailableOnCurrentPlatform()
    {
        environmentProvider.CurrentOperatingSystem.Returns(OperatingSystemType.Linux);

        var macOnlyModule = Substitute.For<ICleanupModule>();
        new CleanupModuleBuilder(macOnlyModule)
            .ForModule(CleanupModuleName.Homebrew)
            .WithDescription("Homebrew cache cleanup")
            .NotAvailableOn(OperatingSystemType.Linux)
            .Configure();

        var crossPlatformModule = Substitute.For<ICleanupModule>();
        new CleanupModuleBuilder(crossPlatformModule)
            .ForModule(CleanupModuleName.Docker)
            .WithDescription("Docker cache cleanup")
            .AvailableOn(OperatingSystemType.Linux)
            .Configure();

        registry.Register(macOnlyModule);
        registry.Register(crossPlatformModule);
        var useCase = new AvailableModulesUseCase(registry, environmentProvider);

        var descriptors = useCase.Invoke();

        descriptors.Should().HaveCount(1);
        descriptors[0].Name().Should().Be(CleanupModuleName.Docker);
    }

    [Fact]
    public void ReturnDescriptorsWithCorrectModuleInfo()
    {
        environmentProvider.CurrentOperatingSystem.Returns(OperatingSystemType.MacOS);

        var dockerModule = Substitute.For<ICleanupModule>();
        new CleanupModuleBuilder(dockerModule)
            .ForModule(CleanupModuleName.Docker)
            .WithDescription("Docker cache cleanup")
            .Destructive()
            .AvailableOn(OperatingSystemType.MacOS)
            .Configure();

        registry.Register(dockerModule);
        var useCase = new AvailableModulesUseCase(registry, environmentProvider);

        var descriptors = useCase.Invoke();

        var descriptor = descriptors[0];

        descriptors.Should().HaveCount(1);
        descriptor.Name().Should().Be(CleanupModuleName.Docker);
        descriptor.Description().Should().Be("Docker cache cleanup");
        descriptor.IsDestructive().Should().BeTrue();
    }

    [Fact]
    public void FilterByMacOSPlatform()
    {
        environmentProvider.CurrentOperatingSystem.Returns(OperatingSystemType.MacOS);

        var macModule = Substitute.For<ICleanupModule>();
        new CleanupModuleBuilder(macModule)
            .ForModule(CleanupModuleName.Homebrew)
            .WithDescription("Homebrew cache cleanup")
            .AvailableOn(OperatingSystemType.MacOS)
            .Configure();

        var linuxOnlyModule = Substitute.For<ICleanupModule>();
        new CleanupModuleBuilder(linuxOnlyModule)
            .ForModule(CleanupModuleName.Docker)
            .WithDescription("Docker cache cleanup")
            .NotAvailableOn(OperatingSystemType.MacOS)
            .Configure();

        registry.Register(macModule);
        registry.Register(linuxOnlyModule);
        var useCase = new AvailableModulesUseCase(registry, environmentProvider);

        var descriptors = useCase.Invoke();

        descriptors.Should().HaveCount(1);
        descriptors[0].Name().Should().Be(CleanupModuleName.Homebrew);
    }
}
