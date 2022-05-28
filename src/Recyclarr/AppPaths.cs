using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using TrashLib;

namespace Recyclarr;

public class AppPaths : IAppPaths
{
    private readonly IFileSystem _fs;
    private string? _appDataPath;

    public AppPaths(IFileSystem fs)
    {
        _fs = fs;
    }

    public string DefaultConfigFilename => "recyclarr.yml";
    public string DefaultAppDataDirectoryName => "recyclarr";

    public bool IsAppDataPathValid => _appDataPath is not null;
    public void SetAppDataPath(string path) => _appDataPath = path;

    [SuppressMessage("Design", "CA1024:Use properties where appropriate")]
    public string GetAppDataPath()
        => _appDataPath ?? throw new DirectoryNotFoundException("Application data directory not set!");

    public string ConfigPath => _fs.Path.Combine(GetAppDataPath(), DefaultConfigFilename);
    public string SettingsPath => _fs.Path.Combine(GetAppDataPath(), "settings.yml");
    public string LogDirectory => _fs.Path.Combine(GetAppDataPath(), "logs");
    public string RepoDirectory => _fs.Path.Combine(GetAppDataPath(), "repo");
    public string CacheDirectory => _fs.Path.Combine(GetAppDataPath(), "cache");
}
