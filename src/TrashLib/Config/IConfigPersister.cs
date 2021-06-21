using System.Collections.Generic;

namespace TrashLib.Config
{
    public interface IConfigPersister<T>
        where T : IServiceConfiguration
    {
        ICollection<T> Load();
        void Save(IEnumerable<T> settings);
    }
}
