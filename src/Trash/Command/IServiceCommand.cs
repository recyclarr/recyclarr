using System.Collections.Generic;

namespace Trash.Command
{
    public interface IServiceCommand
    {
        bool Preview { get; }
        bool Debug { get; }
        List<string>? Config { get; }
        string CacheStoragePath { get; }
    }
}
