using System.Collections.Generic;
using TrashLib.Config;

namespace Recyclarr.Code.Settings.Persisters
{
    public interface IConfigPersister<T>
        where T : IServiceConfiguration
    {
        ICollection<T> Load();
        void Save(IEnumerable<T> settings);
    }
}
