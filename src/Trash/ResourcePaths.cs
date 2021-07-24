using TrashLib.Radarr.Config;

namespace Trash
{
    public class ResourcePaths : IResourcePaths
    {
        public string RepoPath => AppPaths.RepoDirectory;
    }
}
