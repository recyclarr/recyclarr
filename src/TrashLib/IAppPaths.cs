using System.IO.Abstractions;

namespace TrashLib;

public interface IAppPaths
{
    IDirectoryInfo AppDataDirectory { get; }
    IFileInfo ConfigPath { get; }
    IFileInfo SettingsPath { get; }
    IDirectoryInfo LogDirectory { get; }
    IDirectoryInfo RepoDirectory { get; }
    IDirectoryInfo CacheDirectory { get; }
}
