using System.IO.Abstractions;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Startup;

namespace Recyclarr.TrashLib;

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
    public IFileInfo SecretsPath => AppDataDirectory.File("secrets.yml");
    public IDirectoryInfo LogDirectory => AppDataDirectory.SubDir("logs", "cli");
    public IDirectoryInfo ReposDirectory => AppDataDirectory.SubDir("repositories");
    public IDirectoryInfo CacheDirectory => AppDataDirectory.SubDir("cache");
    public IDirectoryInfo ConfigsDirectory => AppDataDirectory.SubDir("configs");
}
