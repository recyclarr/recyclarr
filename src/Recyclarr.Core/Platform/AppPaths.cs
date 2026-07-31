using System.IO.Abstractions;

namespace Recyclarr.Platform;

public class AppPaths(IDirectoryInfo configRoot, IDirectoryInfo dataRoot) : IAppPaths
{
    public static string DefaultAppDataDirectoryName => "recyclarr";

    // Config root for backward compatibility with other code
    public IDirectoryInfo ConfigDirectory => configRoot;

    // Ephemeral data directories (derive from data root)
    public IDirectoryInfo CliLogDirectory => dataRoot.SubDirectory("logs", "cli");
    public IDirectoryInfo ServerLogDirectory => dataRoot.SubDirectory("logs", "server");
    public IDirectoryInfo ResourceDirectory => dataRoot.SubDirectory("resources");

    // User configuration directories (derive from config root)
    public IDirectoryInfo StateDirectory => configRoot.SubDirectory("state");
    public IDirectoryInfo YamlConfigDirectory => configRoot.SubDirectory("configs");
    public IDirectoryInfo YamlIncludeDirectory => configRoot.SubDirectory("includes");

    // Do not initialize the repo directory here; the RepoUpdater handles that later.
    public void CreateTopDirectories()
    {
        StateDirectory.Create();
        CliLogDirectory.Create();
        ServerLogDirectory.Create();
        YamlConfigDirectory.Create();
        YamlIncludeDirectory.Create();
    }
}
