using System.Collections.Generic;

namespace Trash.Command
{
    public interface IBaseCommand
    {
        bool Preview { get; }
        bool Debug { get; }
        List<string>? Config { get; }
    }
}
