using TrashLib.Cache;

namespace Trash.Command.Helpers
{
    public class CacheStoragePath : ICacheStoragePath
    {
        private readonly IActiveServiceCommandProvider _serviceCommandProvider;

        public CacheStoragePath(IActiveServiceCommandProvider serviceCommandProvider)
        {
            _serviceCommandProvider = serviceCommandProvider;
        }

        public string Path => _serviceCommandProvider.ActiveCommand.CacheStoragePath;
    }
}
