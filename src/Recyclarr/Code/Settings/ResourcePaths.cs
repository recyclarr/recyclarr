using Recyclarr.Code.Settings.Persisters;
using TrashLib.Config;

namespace Recyclarr.Code.Settings
{
    public class ResourcePaths : IResourcePaths
    {
        private readonly AppSettings _appSettings;

        public ResourcePaths(IAppSettingsPersister appSettingsPersister)
        {
            _appSettings = appSettingsPersister.Load();
        }

        public string RepoPath => _appSettings.RepoPath;
    }
}
