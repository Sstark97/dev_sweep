namespace DevSweep.Infrastructure.Environment;

internal static class MacOsPaths
{
    internal static string JetBrainsBase(string home) =>
        Path.Combine(home, "Library", "Application Support", "JetBrains");

    internal static string DockerConfig(string home) =>
        Path.Combine(home, ".docker");

    internal static string MavenRepository(string home) =>
        Path.Combine(home, ".m2", "repository");

    internal static string GradleCache(string home) =>
        Path.Combine(home, ".gradle", "caches");

    internal static string NodeModulesGlobal(string home) =>
        Path.Combine(home, ".npm", "_cacache");

    internal static string PythonCache(string home) =>
        Path.Combine(home, "Library", "Caches", "pip");

    internal static string Sdkman(string home) =>
        Path.Combine(home, ".sdkman", "tmp");

    internal static string HomebrewCache(string home) =>
        Path.Combine(home, "Library", "Caches", "Homebrew");

    internal static string SystemTemp() => "/tmp";

    internal static string SystemLogs() => "/private/var/log";

    internal static string SystemCache(string home) =>
        Path.Combine(home, "Library", "Caches");
}
