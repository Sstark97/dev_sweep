namespace DevSweep.Infrastructure.Environment;

internal static class WindowsPaths
{
    internal static string JetBrainsBase() =>
        Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "JetBrains");

    internal static string JetBrainsCache() =>
        Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "JetBrains", "caches");

    internal static string DockerConfig(string home) =>
        Path.Combine(home, ".docker");

    internal static string MavenRepository(string home) =>
        Path.Combine(home, ".m2", "repository");

    internal static string GradleCache(string home) =>
        Path.Combine(home, ".gradle", "caches");

    internal static string NodeModulesGlobal() =>
        Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
            "npm-cache");

    internal static string PythonCache() =>
        Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "pip", "Cache");

    internal static string Sdkman(string home) =>
        Path.Combine(home, ".sdkman", "tmp");

    internal static string HomebrewCache() =>
        Path.GetTempPath();

    internal static string SystemTemp() =>
        Path.GetTempPath();

    internal static string SystemLogs()
    {
        var windowsRoot = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows);
        return !string.IsNullOrEmpty(windowsRoot)
            ? Path.Combine(windowsRoot, "System32", "winevt", "Logs")
            : Path.GetTempPath();
    }

    internal static string SystemCache() =>
        Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "Temp");
}
