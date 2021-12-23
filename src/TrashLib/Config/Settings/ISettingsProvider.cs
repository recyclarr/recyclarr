namespace TrashLib.Config.Settings;

public interface ISettingsProvider
{
    SettingsValues Settings { get; }
    void UseSettings(SettingsValues settings);
}
