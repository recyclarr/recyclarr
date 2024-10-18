using System.IO.Abstractions;
using Recyclarr.Common.Extensions;
using Recyclarr.Platform;
using Recyclarr.Yaml;
using YamlDotNet.Core;

namespace Recyclarr.Settings;

public class SettingsLoader(IAppPaths paths, IYamlSerializerFactory serializerFactory)
{
    public RecyclarrSettings LoadAndOptionallyCreate()
    {
        var yamlPath = paths.AppDataDirectory.YamlFile("settings") ?? CreateDefaultSettingsFile();

        try
        {
            using var stream = yamlPath.OpenText();
            var deserializer = serializerFactory.CreateDeserializer();
            return deserializer.Deserialize<RecyclarrSettings?>(stream.ReadToEnd()) ?? new RecyclarrSettings();
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

        var settingsFile = paths.AppDataDirectory.File("settings.yml");
        settingsFile.CreateParentDirectory();
        using var stream = settingsFile.CreateText();
        stream.Write(fileData);
        return settingsFile;
    }
}
