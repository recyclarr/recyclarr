using System.Collections.Generic;

namespace Trash.Command.Helpers
{
    public interface IServiceCommand
    {
        bool Preview { get; }
        bool Debug { get; }
        ICollection<string>? Config { get; }
        string CacheStoragePath { get; }
    }
}
