namespace Recyclarr.Settings;

internal class SettingsProvider(SettingsLoader loader)
{
    private readonly Lazy<RecyclarrSettings> _settings = new(loader.LoadAndOptionallyCreate);
    public RecyclarrSettings Settings => _settings.Value;
}
