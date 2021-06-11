using System;
using TrashLib.Cache;

namespace Trash.Command.Helpers
{
    public class CacheStoragePath : ICacheStoragePath
    {
        private readonly Lazy<IServiceCommand> _cmd;

        public CacheStoragePath(Lazy<IServiceCommand> cmd)
        {
            _cmd = cmd;
        }

        public string Path => _cmd.Value.CacheStoragePath;
    }
}
