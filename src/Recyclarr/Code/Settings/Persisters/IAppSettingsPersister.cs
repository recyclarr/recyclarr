namespace Recyclarr.Code.Settings.Persisters
{
    public interface IAppSettingsPersister
    {
        AppSettings Load();
        void Save(AppSettings settings);
    }
}
