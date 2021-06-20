using System;
using System.Collections.Generic;

namespace TrashLib.Config
{
    public interface IConfigProvider
    {
        IServiceConfiguration Active { get; set; }
    }

    public interface IConfigProvider<T>
        where T : IServiceConfiguration
    {
        T Active { get; set; }
        ICollection<T> Configs { get; }
        event Action<T?>? ActiveChanged;
    }
}
