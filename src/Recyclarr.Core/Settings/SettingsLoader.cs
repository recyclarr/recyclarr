using System.IO.Abstractions;
using FluentValidation;
using Recyclarr.Common.Extensions;
using Recyclarr.Common.FluentValidation;
using Recyclarr.Platform;
using Recyclarr.Settings.Deprecations;
using Recyclarr.Settings.Models;
using Recyclarr.Yaml;
using YamlDotNet.Core;

namespace Recyclarr.Settings;

public class SettingsLoader(
    IAppPaths paths,
    IYamlSerializerFactory serializerFactory,
    SettingsDeprecations deprecations
)
{
    public RecyclarrSettings LoadAndOptionallyCreate()
    {
        var yamlPath = paths.ConfigDirectory.YamlFile("settings") ?? CreateDefaultSettingsFile();

        try
        {
            using var stream = yamlPath.OpenText();
            var deserializer = serializerFactory.CreateDeserializer(YamlFileType.Settings);
            var settings =
                deserializer.Deserialize<RecyclarrSettings?>(stream.ReadToEnd())
                ?? new RecyclarrSettings();

            // Apply deprecation transformations before validation
            settings = deprecations.CheckAndTransform(settings);

            ValidateSettings(settings);
            return settings;
        }
        catch (YamlException e)
        {
            e.Data["ContextualMessage"] =
                SettingsContextualMessages.GetContextualErrorFromException(e);
            throw;
        }
    }

    private static void ValidateSettings(RecyclarrSettings settings)
    {
        try
        {
            var validator = new RecyclarrSettingsValidator();
            validator.ValidateAndThrow(settings);
        }
        catch (ValidationException e)
        {
            throw new ContextualValidationException(e, "Settings", "Settings Validation");
        }
    }

    private IFileInfo CreateDefaultSettingsFile()
    {
        const string fileData = """
            # yaml-language-server: $schema=https://schemas.recyclarr.dev/latest/settings-schema.json

            # Edit this file to customize the behavior of Recyclarr beyond its defaults
            # For the settings file reference guide, visit the link to the wiki below:
            # https://recyclarr.dev/reference/settings/
            """;

        var settingsFile = paths.ConfigDirectory.File("settings.yml");
        settingsFile.CreateParentDirectory();
        using var stream = settingsFile.CreateText();
        stream.Write(fileData);
        return settingsFile;
    }
}
