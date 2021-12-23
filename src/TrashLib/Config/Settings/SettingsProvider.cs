namespace TrashLib.Config.Settings;

public class SettingsProvider : ISettingsProvider
{
    public SettingsValues Settings { get; private set; } = new();

    public void UseSettings(SettingsValues settings)
    {
        Settings = settings;
    }
}
