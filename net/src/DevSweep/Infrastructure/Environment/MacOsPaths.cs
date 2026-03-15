namespace DevSweep.Infrastructure.Environment;

internal static class MacOsPaths
{
    internal static string JetBrainsBase(string home) =>
        Path.Combine(home, "Library", "Application Support", "JetBrains");

    internal static string JetBrainsCache(string home) =>
        Path.Combine(home, "Library", "Caches", "JetBrains");

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

    internal static string GradleWrapper(string home) =>
        Path.Combine(home, ".gradle", "wrapper");

    internal static string NvmCache(string home) =>
        Path.Combine(home, ".nvm", ".cache");

    internal static string NpmFull(string home) =>
        Path.Combine(home, ".npm");

    internal static string YarnCache(string home) =>
        Path.Combine(home, "Library", "Caches", "Yarn");

    internal static string PnpmStore(string home) =>
        Path.Combine(home, "Library", "pnpm", "store");

    internal static string PoetryCache(string home) =>
        Path.Combine(home, "Library", "Caches", "pypoetry");

    internal static string SystemTemp() => "/tmp";

    internal static string SystemLogs() => "/private/var/log";

    internal static string SystemCache(string home) =>
        Path.Combine(home, "Library", "Caches");
}
