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

    public FilePath JetBrainsBasePath() => ResolvePath(
        MacOsPaths.JetBrainsBase,
        LinuxPaths.JetBrainsBase,
        _ => WindowsPaths.JetBrainsBase());

    public FilePath JetBrainsCachePath() => ResolvePath(
        MacOsPaths.JetBrainsCache,
        LinuxPaths.JetBrainsCache,
        _ => WindowsPaths.JetBrainsCache());

    public FilePath DockerConfigPath() => ResolvePath(
        MacOsPaths.DockerConfig,
        LinuxPaths.DockerConfig,
        WindowsPaths.DockerConfig);

    public FilePath MavenRepositoryPath() => ResolvePath(
        MacOsPaths.MavenRepository,
        LinuxPaths.MavenRepository,
        WindowsPaths.MavenRepository);

    public FilePath GradleCachePath() => ResolvePath(
        MacOsPaths.GradleCache,
        LinuxPaths.GradleCache,
        WindowsPaths.GradleCache);

    public FilePath NodeModulesGlobalPath() => ResolvePath(
        MacOsPaths.NodeModulesGlobal,
        LinuxPaths.NodeModulesGlobal,
        _ => WindowsPaths.NodeModulesGlobal());

    public FilePath PythonCachePath() => ResolvePath(
        MacOsPaths.PythonCache,
        LinuxPaths.PythonCache,
        _ => WindowsPaths.PythonCache());

    public FilePath SdkmanPath() => ResolvePath(
        MacOsPaths.Sdkman,
        LinuxPaths.Sdkman,
        WindowsPaths.Sdkman);

    public FilePath HomebrewCachePath() => ResolvePath(
        MacOsPaths.HomebrewCache,
        LinuxPaths.HomebrewCache,
        _ => WindowsPaths.HomebrewCache());

    public FilePath GradleWrapperPath() => ResolvePath(
        MacOsPaths.GradleWrapper,
        LinuxPaths.GradleWrapper,
        WindowsPaths.GradleWrapper);

    public FilePath NvmCachePath() => ResolvePath(
        MacOsPaths.NvmCache,
        LinuxPaths.NvmCache,
        WindowsPaths.NvmCache);

    public FilePath NpmFullPath() => ResolvePath(
        MacOsPaths.NpmFull,
        LinuxPaths.NpmFull,
        _ => WindowsPaths.NpmFull());

    public FilePath YarnCachePath() => ResolvePath(
        MacOsPaths.YarnCache,
        LinuxPaths.YarnCache,
        _ => WindowsPaths.YarnCache());

    public FilePath PnpmStorePath() => ResolvePath(
        MacOsPaths.PnpmStore,
        LinuxPaths.PnpmStore,
        _ => WindowsPaths.PnpmStore());

    public FilePath PoetryCachePath() => ResolvePath(
        MacOsPaths.PoetryCache,
        LinuxPaths.PoetryCache,
        _ => WindowsPaths.PoetryCache());

    public FilePath SystemTempPath() => ResolvePath(
        _ => MacOsPaths.SystemTemp(),
        _ => LinuxPaths.SystemTemp(),
        _ => WindowsPaths.SystemTemp());

    public FilePath SystemLogsPath() => ResolvePath(
        _ => MacOsPaths.SystemLogs(),
        _ => LinuxPaths.SystemLogs(),
        _ => WindowsPaths.SystemLogs());

    public int StaleProjectDays => 90;

    public int StaleProjectMaxDepth => 6;

    public FilePath SystemCachePath() => ResolvePath(
        MacOsPaths.SystemCache,
        LinuxPaths.SystemCache,
        _ => WindowsPaths.SystemCache());

    private FilePath ResolvePath(
        Func<string, string> macOs,
        Func<string, string> linux,
        Func<string, string> windows) =>
        ToFilePath(operatingSystem switch
        {
            OperatingSystemType.MacOS => macOs(homeDirectory),
            OperatingSystemType.Linux => linux(homeDirectory),
            OperatingSystemType.Windows => windows(homeDirectory),
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
