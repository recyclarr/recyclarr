using System.IO.Abstractions;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Startup;
using Recyclarr.Yaml;
using YamlDotNet.Core;

namespace Recyclarr.TrashLib.Settings;

public class SettingsProvider : ISettingsProvider
{
    public SettingsValues Settings => _settings.Value;

    private readonly ILogger _log;
    private readonly IAppPaths _paths;
    private readonly Lazy<SettingsValues> _settings;

    public SettingsProvider(ILogger log, IAppPaths paths, IYamlSerializerFactory serializerFactory)
    {
        _log = log;
        _paths = paths;
        _settings = new Lazy<SettingsValues>(() => LoadOrCreateSettingsFile(serializerFactory));
    }

    private SettingsValues LoadOrCreateSettingsFile(IYamlSerializerFactory serializerFactory)
    {
        var yamlPath = _paths.AppDataDirectory.YamlFile("settings");
        if (yamlPath is null)
        {
            yamlPath = CreateDefaultSettingsFile();
        }

        try
        {
            using var stream = yamlPath.OpenText();
            var deserializer = serializerFactory.CreateDeserializer();
            return deserializer.Deserialize<SettingsValues?>(stream.ReadToEnd()) ?? new SettingsValues();
        }
        catch (YamlException e)
        {
            _log.Debug(e, "Exception while parsing settings file");

            var line = e.Start.Line;
            var msg = SettingsContextualMessages.GetContextualErrorFromException(e);
            _log.Error("Exception while parsing settings.yml at line {Line}: {Msg}", line, msg);

            throw;
        }
    }

    private IFileInfo CreateDefaultSettingsFile()
    {
        const string fileData =
            "# yaml-language-server: $schema=https://raw.githubusercontent.com/recyclarr/recyclarr/master/schemas/settings-schema.json\n" +
            "\n" +
            "# Edit this file to customize the behavior of Recyclarr beyond its defaults\n" +
            "# For the settings file reference guide, visit the link to the wiki below:\n" +
            "# https://recyclarr.dev/wiki/yaml/settings-reference/\n";

        var settingsFile = _paths.AppDataDirectory.File("settings.yml");
        settingsFile.CreateParentDirectory();
        using var stream = settingsFile.CreateText();
        stream.Write(fileData);
        return settingsFile;
    }
}
