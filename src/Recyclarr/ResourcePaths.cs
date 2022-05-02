using TrashLib.Radarr.Config;

namespace Recyclarr;

public class ResourcePaths : IResourcePaths
{
    public string RepoPath => AppPaths.RepoDirectory;
    public string SettingsPath => AppPaths.DefaultSettingsPath;
}
