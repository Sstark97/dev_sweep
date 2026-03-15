using System.Runtime.InteropServices;
using DevSweep.Application.Ports.Driven;
using DevSweep.Domain.Enums;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Infrastructure.Environment;

public sealed class SystemEnvironmentProvider : IEnvironmentProvider
{
    private readonly OperatingSystemType operatingSystem;
    private readonly string homeDirectory;

    public SystemEnvironmentProvider()
    {
        operatingSystem = DetectOperatingSystem();
        homeDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
    }

    public OperatingSystemType CurrentOperatingSystem => operatingSystem;

    public FilePath HomePath => ToFilePath(homeDirectory);

    public FilePath JetBrainsBasePath() => ToFilePath(
        operatingSystem switch
        {
            OperatingSystemType.MacOS => MacOsPaths.JetBrainsBase(homeDirectory),
            OperatingSystemType.Linux => LinuxPaths.JetBrainsBase(homeDirectory),
            OperatingSystemType.Windows => WindowsPaths.JetBrainsBase(),
            _ => throw new PlatformNotSupportedException($"Unsupported operating system: {operatingSystem}")
        });

    public FilePath JetBrainsCachePath() => ToFilePath(
        operatingSystem switch
        {
            OperatingSystemType.MacOS => MacOsPaths.JetBrainsCache(homeDirectory),
            OperatingSystemType.Linux => LinuxPaths.JetBrainsCache(homeDirectory),
            OperatingSystemType.Windows => WindowsPaths.JetBrainsCache(),
            _ => throw new PlatformNotSupportedException($"Unsupported operating system: {operatingSystem}")
        });

    public FilePath DockerConfigPath() => ToFilePath(
        operatingSystem switch
        {
            OperatingSystemType.MacOS => MacOsPaths.DockerConfig(homeDirectory),
            OperatingSystemType.Linux => LinuxPaths.DockerConfig(homeDirectory),
            OperatingSystemType.Windows => WindowsPaths.DockerConfig(homeDirectory),
            _ => throw new PlatformNotSupportedException($"Unsupported operating system: {operatingSystem}")
        });

    public FilePath MavenRepositoryPath() => ToFilePath(
        operatingSystem switch
        {
            OperatingSystemType.MacOS => MacOsPaths.MavenRepository(homeDirectory),
            OperatingSystemType.Linux => LinuxPaths.MavenRepository(homeDirectory),
            OperatingSystemType.Windows => WindowsPaths.MavenRepository(homeDirectory),
            _ => throw new PlatformNotSupportedException($"Unsupported operating system: {operatingSystem}")
        });

    public FilePath GradleCachePath() => ToFilePath(
        operatingSystem switch
        {
            OperatingSystemType.MacOS => MacOsPaths.GradleCache(homeDirectory),
            OperatingSystemType.Linux => LinuxPaths.GradleCache(homeDirectory),
            OperatingSystemType.Windows => WindowsPaths.GradleCache(homeDirectory),
            _ => throw new PlatformNotSupportedException($"Unsupported operating system: {operatingSystem}")
        });

    public FilePath NodeModulesGlobalPath() => ToFilePath(
        operatingSystem switch
        {
            OperatingSystemType.MacOS => MacOsPaths.NodeModulesGlobal(homeDirectory),
            OperatingSystemType.Linux => LinuxPaths.NodeModulesGlobal(homeDirectory),
            OperatingSystemType.Windows => WindowsPaths.NodeModulesGlobal(),
            _ => throw new PlatformNotSupportedException($"Unsupported operating system: {operatingSystem}")
        });

    public FilePath PythonCachePath() => ToFilePath(
        operatingSystem switch
        {
            OperatingSystemType.MacOS => MacOsPaths.PythonCache(homeDirectory),
            OperatingSystemType.Linux => LinuxPaths.PythonCache(homeDirectory),
            OperatingSystemType.Windows => WindowsPaths.PythonCache(),
            _ => throw new PlatformNotSupportedException($"Unsupported operating system: {operatingSystem}")
        });

    public FilePath SdkmanPath() => ToFilePath(
        operatingSystem switch
        {
            OperatingSystemType.MacOS => MacOsPaths.Sdkman(homeDirectory),
            OperatingSystemType.Linux => LinuxPaths.Sdkman(homeDirectory),
            OperatingSystemType.Windows => WindowsPaths.Sdkman(homeDirectory),
            _ => throw new PlatformNotSupportedException($"Unsupported operating system: {operatingSystem}")
        });

    public FilePath HomebrewCachePath() => ToFilePath(
        operatingSystem switch
        {
            OperatingSystemType.MacOS => MacOsPaths.HomebrewCache(homeDirectory),
            OperatingSystemType.Linux => LinuxPaths.HomebrewCache(homeDirectory),
            OperatingSystemType.Windows => WindowsPaths.HomebrewCache(),
            _ => throw new PlatformNotSupportedException($"Unsupported operating system: {operatingSystem}")
        });

    public FilePath GradleWrapperPath() => ToFilePath(
        operatingSystem switch
        {
            OperatingSystemType.MacOS => MacOsPaths.GradleWrapper(homeDirectory),
            OperatingSystemType.Linux => LinuxPaths.GradleWrapper(homeDirectory),
            OperatingSystemType.Windows => WindowsPaths.GradleWrapper(homeDirectory),
            _ => throw new PlatformNotSupportedException($"Unsupported operating system: {operatingSystem}")
        });

    public FilePath NvmCachePath() => ToFilePath(
        operatingSystem switch
        {
            OperatingSystemType.MacOS => MacOsPaths.NvmCache(homeDirectory),
            OperatingSystemType.Linux => LinuxPaths.NvmCache(homeDirectory),
            OperatingSystemType.Windows => WindowsPaths.NvmCache(homeDirectory),
            _ => throw new PlatformNotSupportedException($"Unsupported operating system: {operatingSystem}")
        });

    public FilePath NpmFullPath() => ToFilePath(
        operatingSystem switch
        {
            OperatingSystemType.MacOS => MacOsPaths.NpmFull(homeDirectory),
            OperatingSystemType.Linux => LinuxPaths.NpmFull(homeDirectory),
            OperatingSystemType.Windows => WindowsPaths.NpmFull(),
            _ => throw new PlatformNotSupportedException($"Unsupported operating system: {operatingSystem}")
        });

    public FilePath YarnCachePath() => ToFilePath(
        operatingSystem switch
        {
            OperatingSystemType.MacOS => MacOsPaths.YarnCache(homeDirectory),
            OperatingSystemType.Linux => LinuxPaths.YarnCache(homeDirectory),
            OperatingSystemType.Windows => WindowsPaths.YarnCache(),
            _ => throw new PlatformNotSupportedException($"Unsupported operating system: {operatingSystem}")
        });

    public FilePath PnpmStorePath() => ToFilePath(
        operatingSystem switch
        {
            OperatingSystemType.MacOS => MacOsPaths.PnpmStore(homeDirectory),
            OperatingSystemType.Linux => LinuxPaths.PnpmStore(homeDirectory),
            OperatingSystemType.Windows => WindowsPaths.PnpmStore(),
            _ => throw new PlatformNotSupportedException($"Unsupported operating system: {operatingSystem}")
        });

    public FilePath PoetryCachePath() => ToFilePath(
        operatingSystem switch
        {
            OperatingSystemType.MacOS => MacOsPaths.PoetryCache(homeDirectory),
            OperatingSystemType.Linux => LinuxPaths.PoetryCache(homeDirectory),
            OperatingSystemType.Windows => WindowsPaths.PoetryCache(),
            _ => throw new PlatformNotSupportedException($"Unsupported operating system: {operatingSystem}")
        });

    public FilePath SystemTempPath() => ToFilePath(
        operatingSystem switch
        {
            OperatingSystemType.MacOS => MacOsPaths.SystemTemp(),
            OperatingSystemType.Linux => LinuxPaths.SystemTemp(),
            OperatingSystemType.Windows => WindowsPaths.SystemTemp(),
            _ => throw new PlatformNotSupportedException($"Unsupported operating system: {operatingSystem}")
        });

    public FilePath SystemLogsPath() => ToFilePath(
        operatingSystem switch
        {
            OperatingSystemType.MacOS => MacOsPaths.SystemLogs(),
            OperatingSystemType.Linux => LinuxPaths.SystemLogs(),
            OperatingSystemType.Windows => WindowsPaths.SystemLogs(),
            _ => throw new PlatformNotSupportedException($"Unsupported operating system: {operatingSystem}")
        });

    public FilePath SystemCachePath() => ToFilePath(
        operatingSystem switch
        {
            OperatingSystemType.MacOS => MacOsPaths.SystemCache(homeDirectory),
            OperatingSystemType.Linux => LinuxPaths.SystemCache(homeDirectory),
            OperatingSystemType.Windows => WindowsPaths.SystemCache(),
            _ => throw new PlatformNotSupportedException($"Unsupported operating system: {operatingSystem}")
        });

    private static OperatingSystemType DetectOperatingSystem()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return OperatingSystemType.MacOS;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return OperatingSystemType.Linux;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return OperatingSystemType.Windows;

        throw new PlatformNotSupportedException("Unsupported operating system");
    }

    private static FilePath ToFilePath(string path) => FilePath.Create(path).Value;
}
