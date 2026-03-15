using DevSweep.Domain.Enums;
using DevSweep.Domain.ValueObjects;

namespace DevSweep.Application.Ports.Driven;

public interface IEnvironmentProvider
{
    OperatingSystemType CurrentOperatingSystem { get; }
    FilePath HomePath { get; }
    FilePath JetBrainsBasePath();
    FilePath JetBrainsCachePath();
    FilePath DockerConfigPath();
    FilePath MavenRepositoryPath();
    FilePath GradleCachePath();
    FilePath NodeModulesGlobalPath();
    FilePath PythonCachePath();
    FilePath SdkmanPath();
    FilePath HomebrewCachePath();
    FilePath GradleWrapperPath();
    FilePath NvmCachePath();
    FilePath NpmFullPath();
    FilePath YarnCachePath();
    FilePath PnpmStorePath();
    FilePath PoetryCachePath();
    FilePath SystemTempPath();
    FilePath SystemLogsPath();
    FilePath SystemCachePath();
}
