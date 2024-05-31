using System.IO.Abstractions;
using Recyclarr.Common.Extensions;
using Recyclarr.Platform;
using Recyclarr.Yaml;
using YamlDotNet.Core;

namespace Recyclarr.Settings;

public class SettingsProvider : ISettingsProvider
{
    private readonly IAppPaths _paths;
    private readonly Lazy<SettingsValues> _settings;

    public SettingsValues Settings => _settings.Value;

    public SettingsProvider(IAppPaths paths, IYamlSerializerFactory serializerFactory)
    {
        _paths = paths;
        _settings = new Lazy<SettingsValues>(() => LoadOrCreateSettingsFile(serializerFactory));
    }

    private SettingsValues LoadOrCreateSettingsFile(IYamlSerializerFactory serializerFactory)
    {
        var yamlPath = _paths.AppDataDirectory.YamlFile("settings") ?? CreateDefaultSettingsFile();

        try
        {
            using var stream = yamlPath.OpenText();
            var deserializer = serializerFactory.CreateDeserializer();
            return deserializer.Deserialize<SettingsValues?>(stream.ReadToEnd()) ?? new SettingsValues();
        }
        catch (YamlException e)
        {
            e.Data["ContextualMessage"] = SettingsContextualMessages.GetContextualErrorFromException(e);
            throw;
        }
    }

    private IFileInfo CreateDefaultSettingsFile()
    {
        const string fileData =
            """
            # yaml-language-server: $schema=https://raw.githubusercontent.com/recyclarr/recyclarr/master/schemas/settings-schema.json

            # Edit this file to customize the behavior of Recyclarr beyond its defaults
            # For the settings file reference guide, visit the link to the wiki below:
            # https://recyclarr.dev/wiki/yaml/settings-reference/
            """;

        var settingsFile = _paths.AppDataDirectory.File("settings.yml");
        settingsFile.CreateParentDirectory();
        using var stream = settingsFile.CreateText();
        stream.Write(fileData);
        return settingsFile;
    }
}
