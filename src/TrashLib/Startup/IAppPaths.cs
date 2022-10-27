using System.IO.Abstractions;

namespace TrashLib.Startup;

public interface IAppPaths
{
    IDirectoryInfo AppDataDirectory { get; }
    IFileInfo ConfigPath { get; }
    IFileInfo SettingsPath { get; }
    IFileInfo SecretsPath { get; }
    IDirectoryInfo LogDirectory { get; }
    IDirectoryInfo RepoDirectory { get; }
    IDirectoryInfo CacheDirectory { get; }
    IDirectoryInfo ConfigsDirectory { get; }
}
