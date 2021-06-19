using System.Collections.Generic;
using TrashLib.Radarr.Config;

namespace Recyclarr.Code.Settings.Persisters
{
    public interface IRadarrConfigPersister
    {
        ICollection<RadarrConfiguration> Load();
        void Save(IEnumerable<RadarrConfiguration> settings);
    }
}
