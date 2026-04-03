using DevSweep.Application.Modules;
using DevSweep.Application.Ports.Driven;
using DevSweep.Application.Ports.Driving;
using DevSweep.Application.UseCases;
using DevSweep.Domain.Enums;
using DevSweep.Infrastructure.Cli.Interaction;
using DevSweep.Infrastructure.Cli.Output;
using DevSweep.Infrastructure.Environment;
using DevSweep.Infrastructure.FileSystem;
using DevSweep.Infrastructure.Modules.DevTools;
using DevSweep.Infrastructure.Modules.Docker;
using DevSweep.Infrastructure.Modules.Homebrew;
using DevSweep.Infrastructure.Modules.JetBrains;
using DevSweep.Infrastructure.Modules.Projects;
using DevSweep.Infrastructure.Modules.System;
using DevSweep.Infrastructure.Process;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace DevSweep.Infrastructure.Cli.Composition;

internal static class ServiceRegistration
{
    internal static IServiceCollection AddDevSweepServices(
        this IServiceCollection services,
        OutputStrategy outputStrategy,
        bool autoConfirm)
    {
        services.AddSingleton<IFileSystem, PhysicalFileSystem>();
        services.AddSingleton<IProcessManager, SystemProcessManager>();
        services.AddSingleton<ICommandRunner, SystemCommandRunner>();
        services.AddSingleton<IEnvironmentProvider, SystemEnvironmentProvider>();

        services.AddSingleton<IOutputFormatter>(outputStrategy switch
        {
            OutputStrategy.Plain => _ => new PlainTextOutputFormatter(Console.Out),
            OutputStrategy.Json => _ => new JsonOutputFormatter(Console.Out),
            _ => _ => new SpectreOutputFormatter(AnsiConsole.Console)
        });

        if (autoConfirm)
            services.AddSingleton<IUserInteraction, AutoConfirmInteraction>();
        else
            services.AddSingleton<IUserInteraction>(_ => new InteractiveConsole(AnsiConsole.Console));

        if (autoConfirm)
            services.AddSingleton<IModuleSelector, SelectAllModules>();
        else
            services.AddSingleton<IModuleSelector>(_ => new InteractiveMenu(AnsiConsole.Console));

        services.AddSingleton<DockerCliRuntimeStrategy>();
        services.AddSingleton<OrbStackRuntimeStrategy>();
        services.AddSingleton<IReadOnlyList<IContainerRuntimeStrategy>>(sp =>
        [
            sp.GetRequiredService<DockerCliRuntimeStrategy>(),
            sp.GetRequiredService<OrbStackRuntimeStrategy>()
        ]);

        services.AddSingleton<MavenCleaner>();
        services.AddSingleton<GradleCleaner>();
        services.AddSingleton<NodeCleaner>();
        services.AddSingleton<PythonCleaner>();
        services.AddSingleton<SdkmanCleaner>();
        services.AddSingleton<IReadOnlyList<IDevToolsCleaner>>(sp =>
        [
            sp.GetRequiredService<MavenCleaner>(),
            sp.GetRequiredService<GradleCleaner>(),
            sp.GetRequiredService<NodeCleaner>(),
            sp.GetRequiredService<PythonCleaner>(),
            sp.GetRequiredService<SdkmanCleaner>()
        ]);

        services.AddSingleton<IReadOnlyList<IStaleProjectCleaner>>(_ =>
        [
            new StaleNodeProjectCleaner(),
            new StaleMavenProjectCleaner(),
            new StaleGradleProjectCleaner(),
            new StalePythonProjectCleaner()
        ]);

        services.AddSingleton<SystemCacheCleaner>();
        services.AddSingleton<SystemLogsCleaner>();
        services.AddSingleton<SystemTempCleaner>();
        services.AddSingleton<IReadOnlyList<ISystemCleaner>>(sp =>
        [
            sp.GetRequiredService<SystemCacheCleaner>(),
            sp.GetRequiredService<SystemLogsCleaner>(),
            sp.GetRequiredService<SystemTempCleaner>()
        ]);

        services.AddSingleton<JetBrainsModule>();
        services.AddSingleton<DockerModule>();
        services.AddSingleton<HomebrewModule>();
        services.AddSingleton<DevToolsModule>();
        services.AddSingleton<StaleProjectsModule>();
        services.AddSingleton<SystemModule>();

        services.AddSingleton(sp =>
        {
            var registry = new ModuleRegistry();
            registry.Register(sp.GetRequiredService<JetBrainsModule>());
            registry.Register(sp.GetRequiredService<DockerModule>());
            registry.Register(sp.GetRequiredService<HomebrewModule>());
            registry.Register(sp.GetRequiredService<DevToolsModule>());
            registry.Register(sp.GetRequiredService<StaleProjectsModule>());
            registry.Register(sp.GetRequiredService<SystemModule>());
            return registry;
        });

        services.AddSingleton<IAnalyzeUseCase, AnalyzeUseCase>();
        services.AddSingleton<ICleanupUseCase, CleanupUseCase>();
        services.AddSingleton<IAvailableModulesUseCase, AvailableModulesUseCase>();

        return services;
    }
}
