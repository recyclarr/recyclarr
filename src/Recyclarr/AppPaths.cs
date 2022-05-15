using System.IO.Abstractions;
using TrashLib;

namespace Recyclarr;

internal class AppPaths : IAppPaths
{
    private readonly IFileSystem _fs;

    public AppPaths(IFileSystem fs)
    {
        _fs = fs;
    }

    public void SetAppDataPath(string path) => AppDataPath = path;

    public string AppDataPath { get; private set; } = "";
    public string ConfigPath => _fs.Path.Combine(AppContext.BaseDirectory, "recyclarr.yml");
    public string SettingsPath => _fs.Path.Combine(AppDataPath, "settings.yml");
    public string LogDirectory => _fs.Path.Combine(AppDataPath, "logs");
    public string RepoDirectory => _fs.Path.Combine(AppDataPath, "repo");
    public string CacheDirectory => _fs.Path.Combine(AppDataPath, "cache");
}
