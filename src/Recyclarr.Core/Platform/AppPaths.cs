using System.IO.Abstractions;

namespace Recyclarr.Platform;

/// <summary>
/// Runtime directory layout. The config root holds user files and may be read-only; the data root
/// holds rebuildable data (logs, resource clones); the state root holds data that must persist
/// (sync state and the server database).
/// </summary>
public class AppPaths(IDirectoryInfo configRoot, IDirectoryInfo dataRoot, IDirectoryInfo stateRoot)
    : IAppPaths
{
    public IDirectoryInfo ConfigDirectory => configRoot;
    public IDirectoryInfo YamlConfigDirectory => configRoot.SubDirectory("configs");
    public IDirectoryInfo YamlIncludeDirectory => configRoot.SubDirectory("includes");

    public IDirectoryInfo DataDirectory => dataRoot;
    public IDirectoryInfo ServerLogDirectory => dataRoot.SubDirectory("logs", "server");
    public IDirectoryInfo ResourceDirectory => dataRoot.SubDirectory("resources");

    public IDirectoryInfo StateDirectory => stateRoot;

    /// <summary>
    /// Creates only directories Recyclarr writes to. User config directories are never created so
    /// the config root can be mounted read-only; readers treat them as optional.
    /// </summary>
    public void CreateWritableDirectories()
    {
        StateDirectory.Create();
        ServerLogDirectory.Create();
    }
}
