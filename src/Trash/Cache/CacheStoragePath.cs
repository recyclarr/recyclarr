using System;
using Trash.Command;

namespace Trash.Cache
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
