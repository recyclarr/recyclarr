using System.Collections.Generic;
using TrashLib.Radarr.Config;

namespace Recyclarr.Code.Settings.Persisters
{
    public class RadarrConfigPersister : IRadarrConfigPersister
    {
        private readonly ISettingsPersister _persister;

        public RadarrConfigPersister(ISettingsPersister persister)
        {
            _persister = persister;
        }

        private const string Filename = "radarr.json";

        public ICollection<RadarrConfiguration> Load()
        {
            return _persister.LoadSettings<List<RadarrConfiguration>>(Filename);
        }

        public void Save(IEnumerable<RadarrConfiguration> settings)
        {
            _persister.SaveSettings(Filename, settings);
        }
    }
}
