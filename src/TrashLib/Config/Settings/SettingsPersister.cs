using System.IO.Abstractions;
using TrashLib.Radarr.Config;

namespace TrashLib.Config.Settings;

public class SettingsPersister : ISettingsPersister
{
    private readonly IResourcePaths _paths;
    private readonly ISettingsProvider _settingsProvider;
    private readonly IYamlSerializerFactory _serializerFactory;
    private readonly IFileSystem _fileSystem;

    public SettingsPersister(
        IResourcePaths paths,
        ISettingsProvider settingsProvider,
        IYamlSerializerFactory serializerFactory,
        IFileSystem fileSystem)
    {
        _paths = paths;
        _settingsProvider = settingsProvider;
        _serializerFactory = serializerFactory;
        _fileSystem = fileSystem;
    }

    public void Load()
    {
        var deserializer = _serializerFactory.CreateDeserializer();
        var settings = deserializer.Deserialize<SettingsValues?>(LoadOrCreateSettingsFile()) ?? new SettingsValues();
        _settingsProvider.UseSettings(settings);
    }

    private string LoadOrCreateSettingsFile()
    {
        if (!_fileSystem.File.Exists(_paths.SettingsPath))
        {
            CreateDefaultSettingsFile();
        }

        return _fileSystem.File.ReadAllText(_paths.SettingsPath);
    }

    private void CreateDefaultSettingsFile()
    {
        const string fileData =
            "# yaml-language-server: $schema=https://raw.githubusercontent.com/recyclarr/recyclarr/master/schemas/settings-schema.json\n" +
            "\n" +
            "# Edit this file to customize the behavior of Recyclarr beyond its defaults\n" +
            "# For the settings file reference guide, visit the link to the wiki below:\n" +
            "# https://github.com/recyclarr/recyclarr/wiki/Settings-Reference\n";

        _fileSystem.File.WriteAllText(_paths.SettingsPath, fileData);
    }
}
