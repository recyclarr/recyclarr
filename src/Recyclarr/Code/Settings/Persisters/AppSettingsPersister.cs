namespace Recyclarr.Code.Settings.Persisters
{
    public class AppSettingsPersister : IAppSettingsPersister
    {
        private readonly ISettingsPersister _persister;

        public AppSettingsPersister(ISettingsPersister persister)
        {
            _persister = persister;
        }

        private const string Filename = "app-settings.json";

        public AppSettings Load()
        {
            return _persister.LoadSettings<AppSettings>(Filename);
        }

        public void Save(AppSettings settings)
        {
            _persister.SaveSettings(Filename, settings);
        }
    }
}
