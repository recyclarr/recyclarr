namespace Recyclarr.Code.Settings
{
    public interface ISettingsPersister
    {
        T LoadSettings<T>(string jsonFile)
            where T : new();

        void SaveSettings<T>(string jsonFile, T settingsObject);
    }
}
