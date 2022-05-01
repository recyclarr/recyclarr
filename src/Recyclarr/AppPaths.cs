namespace Recyclarr;

internal static class AppPaths
{
    public static string AppDataPath { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "trash-updater");

    public static string DefaultConfigPath { get; } = Path.Combine(AppContext.BaseDirectory, "recyclarr.yml");

    public static string DefaultSettingsPath { get; } = Path.Combine(AppDataPath, "settings.yml");

    public static string LogDirectory { get; } = Path.Combine(AppDataPath, "logs");

    public static string RepoDirectory { get; } = Path.Combine(AppDataPath, "repo");
}
