using System.IO.Abstractions;

namespace Recyclarr.TrashLib.Startup;

public interface IAppPaths
{
    IDirectoryInfo AppDataDirectory { get; }
    IFileInfo ConfigPath { get; }
    IFileInfo SettingsPath { get; }
    IFileInfo SecretsPath { get; }
    IDirectoryInfo LogDirectory { get; }
    IDirectoryInfo ReposDirectory { get; }
    IDirectoryInfo CacheDirectory { get; }
    IDirectoryInfo ConfigsDirectory { get; }
}
