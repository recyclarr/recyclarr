using TrashLib.Startup;

namespace TrashLib.Config.Settings;

public class SettingsProvider : ISettingsProvider
{
    public SettingsValues Settings => _settings.Value;

    private readonly IAppPaths _paths;
    private readonly Lazy<SettingsValues> _settings;

    public SettingsProvider(IAppPaths paths, IYamlSerializerFactory serializerFactory)
    {
        _paths = paths;
        _settings = new Lazy<SettingsValues>(() => LoadOrCreateSettingsFile(serializerFactory));
    }

    private SettingsValues LoadOrCreateSettingsFile(IYamlSerializerFactory serializerFactory)
    {
        if (!_paths.SettingsPath.Exists)
        {
            CreateDefaultSettingsFile();
        }

        using var stream = _paths.SettingsPath.OpenText();
        var deserializer = serializerFactory.CreateDeserializer();
        return deserializer.Deserialize<SettingsValues?>(stream.ReadToEnd()) ?? new SettingsValues();
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
