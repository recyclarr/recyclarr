using System.IO.Abstractions;
using TrashLib.Startup;

namespace TrashLib;

public class AppPaths : IAppPaths
{
    public AppPaths(IDirectoryInfo appDataPath)
    {
        AppDataDirectory = appDataPath;
    }

    public static string DefaultConfigFilename => "recyclarr.yml";
    public static string DefaultAppDataDirectoryName => "recyclarr";

    public IDirectoryInfo AppDataDirectory { get; }

    public IFileInfo ConfigPath => AppDataDirectory.File(DefaultConfigFilename);
    public IFileInfo SettingsPath => AppDataDirectory.File("settings.yml");
    public IDirectoryInfo LogDirectory => AppDataDirectory.SubDirectory("logs");
    public IDirectoryInfo RepoDirectory => AppDataDirectory.SubDirectory("repo");
    public IDirectoryInfo CacheDirectory => AppDataDirectory.SubDirectory("cache");
}
