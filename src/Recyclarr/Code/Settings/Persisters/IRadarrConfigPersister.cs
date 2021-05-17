using System.Collections.Generic;
using TrashLib.Radarr.Config;

namespace Recyclarr.Code.Settings.Persisters
{
    public interface IRadarrConfigPersister
    {
        IList<RadarrConfiguration> Load();
        void Save(IEnumerable<RadarrConfiguration> settings);
    }
}
