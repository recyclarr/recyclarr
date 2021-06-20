using System.Collections.Generic;
using TrashLib.Config;

namespace Recyclarr.Code.Settings.Persisters
{
    public class ConfigPersister<T> : IConfigPersister<T>
        where T : IServiceConfiguration
    {
        private readonly string _filename;
        private readonly ISettingsPersister _persister;

        public ConfigPersister(string filename, ISettingsPersister persister)
        {
            _filename = filename;
            _persister = persister;
        }

        public ICollection<T> Load()
        {
            return _persister.LoadSettings<List<T>>(_filename);
        }

        public void Save(IEnumerable<T> settings)
        {
            _persister.SaveSettings(_filename, settings);
        }
    }
}
