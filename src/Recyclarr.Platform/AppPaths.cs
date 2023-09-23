using System.IO.Abstractions;
using Recyclarr.Common.Extensions;

namespace Recyclarr.Platform;

public class AppPaths : IAppPaths
{
    public AppPaths(IDirectoryInfo appDataPath)
    {
        AppDataDirectory = appDataPath;
    }

    public static string DefaultAppDataDirectoryName => "recyclarr";

    public IDirectoryInfo AppDataDirectory { get; }
    public IDirectoryInfo LogDirectory => AppDataDirectory.SubDir("logs", "cli");
    public IDirectoryInfo ReposDirectory => AppDataDirectory.SubDir("repositories");
    public IDirectoryInfo CacheDirectory => AppDataDirectory.SubDir("cache");
    public IDirectoryInfo ConfigsDirectory => AppDataDirectory.SubDir("configs");
}
