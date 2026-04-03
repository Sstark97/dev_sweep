using AwesomeAssertions;
using DevSweep.Application.Modules;
using DevSweep.Application.Ports.Driven;
using DevSweep.Application.Ports.Driving;
using DevSweep.Application.UseCases;
using DevSweep.Domain.Enums;
using DevSweep.Infrastructure.Cli.Composition;
using DevSweep.Infrastructure.Cli.Interaction;
using DevSweep.Infrastructure.Cli.Output;
using DevSweep.Infrastructure.Environment;
using DevSweep.Infrastructure.FileSystem;
using DevSweep.Infrastructure.Process;
using Microsoft.Extensions.DependencyInjection;

namespace DevSweep.Tests.Infrastructure.Cli.Composition;

internal sealed class ServiceRegistrationShould
{
    private static readonly int RegisteredModuleCount = new[]
    {
        CleanupModuleName.JetBrains,
        CleanupModuleName.Docker,
        CleanupModuleName.Homebrew,
        CleanupModuleName.DevTools,
        CleanupModuleName.Projects,
        CleanupModuleName.System
    }.Length;

    private static ServiceProvider BuildProvider() =>
        BuildProvider(OutputStrategy.Rich, false);

    private static ServiceProvider BuildProvider(OutputStrategy output) =>
        BuildProvider(output, false);

    private static ServiceProvider BuildProvider(bool autoConfirm) =>
        BuildProvider(OutputStrategy.Rich, autoConfirm);

    private static ServiceProvider BuildProvider(OutputStrategy output, bool autoConfirm) =>
        new ServiceCollection()
            .AddDevSweepServices(output, autoConfirm)
            .BuildServiceProvider();

    [Test]
    public void ProvidePhysicalFileSystem()
    {
        using var provider = BuildProvider();

        var resolved = provider.GetService<IFileSystem>();

        resolved.Should().NotBeNull();
        resolved.Should().BeOfType<PhysicalFileSystem>();
    }

    [Test]
    public void ProvideSystemProcessManager()
    {
        using var provider = BuildProvider();

        var resolved = provider.GetService<IProcessManager>();

        resolved.Should().NotBeNull();
        resolved.Should().BeOfType<SystemProcessManager>();
    }

    [Test]
    public void ProvideSystemCommandRunner()
    {
        using var provider = BuildProvider();

        var resolved = provider.GetService<ICommandRunner>();

        resolved.Should().NotBeNull();
        resolved.Should().BeOfType<SystemCommandRunner>();
    }

    [Test]
    public void ProvideSystemEnvironmentProvider()
    {
        using var provider = BuildProvider();

        var resolved = provider.GetService<IEnvironmentProvider>();

        resolved.Should().NotBeNull();
        resolved.Should().BeOfType<SystemEnvironmentProvider>();
    }

    [Test]
    public void ProvideSpectreOutputFormatterWhenRichStrategy()
    {
        using var provider = BuildProvider(OutputStrategy.Rich);

        var resolved = provider.GetService<IOutputFormatter>();

        resolved.Should().BeOfType<SpectreOutputFormatter>();
    }

    [Test]
    public void ProvidePlainTextOutputFormatterWhenPlainStrategy()
    {
        using var provider = BuildProvider(OutputStrategy.Plain);

        var resolved = provider.GetService<IOutputFormatter>();

        resolved.Should().BeOfType<PlainTextOutputFormatter>();
    }

    [Test]
    public void ProvideJsonOutputFormatterWhenJsonStrategy()
    {
        using var provider = BuildProvider(OutputStrategy.Json);

        var resolved = provider.GetService<IOutputFormatter>();

        resolved.Should().BeOfType<JsonOutputFormatter>();
    }

    [Test]
    public void ProvideAutoConfirmInteractionWhenAutoConfirmEnabled()
    {
        using var provider = BuildProvider(true);

        var resolved = provider.GetService<IUserInteraction>();

        resolved.Should().BeOfType<AutoConfirmInteraction>();
    }

    [Test]
    public void ProvideInteractiveConsoleWhenAutoConfirmDisabled()
    {
        using var provider = BuildProvider(false);

        var resolved = provider.GetService<IUserInteraction>();

        resolved.Should().BeOfType<InteractiveConsole>();
    }

    [Test]
    public void ProvideSelectAllModulesWhenAutoConfirmEnabled()
    {
        using var provider = BuildProvider(true);

        var resolved = provider.GetService<IModuleSelector>();

        resolved.Should().BeOfType<SelectAllModules>();
    }

    [Test]
    public void ProvideInteractiveMenuWhenAutoConfirmDisabled()
    {
        using var provider = BuildProvider(false);

        var resolved = provider.GetService<IModuleSelector>();

        resolved.Should().BeOfType<InteractiveMenu>();
    }

    [Test]
    public void ProvideAnalyzeUseCase()
    {
        using var provider = BuildProvider();

        var resolved = provider.GetService<IAnalyzeUseCase>();

        resolved.Should().NotBeNull();
        resolved.Should().BeOfType<AnalyzeUseCase>();
    }

    [Test]
    public void ProvideCleanupUseCase()
    {
        using var provider = BuildProvider();

        var resolved = provider.GetService<ICleanupUseCase>();

        resolved.Should().NotBeNull();
        resolved.Should().BeOfType<CleanupUseCase>();
    }

    [Test]
    public void ProvideAvailableModulesUseCase()
    {
        using var provider = BuildProvider();

        var resolved = provider.GetService<IAvailableModulesUseCase>();

        resolved.Should().NotBeNull();
        resolved.Should().BeOfType<AvailableModulesUseCase>();
    }

    [Test]
    public void RegisterAllCleanupModules()
    {
        using var provider = BuildProvider();

        var registry = provider.GetRequiredService<ModuleRegistry>();
        var modules = registry.Modules();

        modules.Count.Should().Be(RegisteredModuleCount);
    }
}
