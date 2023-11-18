using System.IO.Abstractions;
using Recyclarr.Common.Extensions;

namespace Recyclarr.Platform;

public class AppPaths(IDirectoryInfo appDataPath) : IAppPaths
{
    public static string DefaultAppDataDirectoryName => "recyclarr";

    public IDirectoryInfo AppDataDirectory { get; } = appDataPath;
    public IDirectoryInfo LogDirectory => AppDataDirectory.SubDir("logs", "cli");
    public IDirectoryInfo ReposDirectory => AppDataDirectory.SubDir("repositories");
    public IDirectoryInfo CacheDirectory => AppDataDirectory.SubDir("cache");
    public IDirectoryInfo ConfigsDirectory => AppDataDirectory.SubDir("configs");
}
