using System.IO.Abstractions;

namespace Recyclarr.Platform;

public class AppPaths(IDirectoryInfo appDataPath) : IAppPaths
{
    public static string DefaultAppDataDirectoryName => "recyclarr";

    public IDirectoryInfo AppDataDirectory { get; } = appDataPath;
    public IDirectoryInfo LogDirectory => AppDataDirectory.SubDirectory("logs", "cli");
    public IDirectoryInfo ReposDirectory => CacheDirectory.SubDirectory("resources");
    public IDirectoryInfo CacheDirectory => AppDataDirectory.SubDirectory("cache");
    public IDirectoryInfo ConfigsDirectory => AppDataDirectory.SubDirectory("configs");
    public IDirectoryInfo IncludesDirectory => AppDataDirectory.SubDirectory("includes");
}
