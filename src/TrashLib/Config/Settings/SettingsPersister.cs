using TrashLib.Startup;

namespace TrashLib.Config.Settings;

public class SettingsPersister : ISettingsPersister
{
    private readonly IAppPaths _paths;
    private readonly ISettingsProvider _settingsProvider;
    private readonly IYamlSerializerFactory _serializerFactory;

    public SettingsPersister(
        IAppPaths paths,
        ISettingsProvider settingsProvider,
        IYamlSerializerFactory serializerFactory)
    {
        _paths = paths;
        _settingsProvider = settingsProvider;
        _serializerFactory = serializerFactory;
    }

    public void Load()
    {
        var deserializer = _serializerFactory.CreateDeserializer();
        var settings = deserializer.Deserialize<SettingsValues?>(LoadOrCreateSettingsFile()) ?? new SettingsValues();
        _settingsProvider.UseSettings(settings);
    }

    private string LoadOrCreateSettingsFile()
    {
        if (!_paths.SettingsPath.Exists)
        {
            CreateDefaultSettingsFile();
        }

        using var stream = _paths.SettingsPath.OpenText();
        return stream.ReadToEnd();
    }

    private void CreateDefaultSettingsFile()
    {
        const string fileData =
            "# yaml-language-server: $schema=https://raw.githubusercontent.com/recyclarr/recyclarr/master/schemas/settings-schema.json\n" +
            "\n" +
            "# Edit this file to customize the behavior of Recyclarr beyond its defaults\n" +
            "# For the settings file reference guide, visit the link to the wiki below:\n" +
            "# https://github.com/recyclarr/recyclarr/wiki/Settings-Reference\n";

        _paths.SettingsPath.Directory.Create();
        using var stream = _paths.SettingsPath.CreateText();
        stream.Write(fileData);
    }
}
