using System.IO.Abstractions;
using FluentValidation;
using Recyclarr.Common.Extensions;
using Recyclarr.Common.FluentValidation;
using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.Logging;
using Recyclarr.Platform;
using Recyclarr.Settings.Deprecations;
using Recyclarr.Settings.Models;
using Recyclarr.Yaml;
using YamlDotNet.Core;

namespace Recyclarr.Settings;

public class SettingsLoader(
    IAppPaths paths,
    IYamlSerializerFactory serializerFactory,
    SettingsDeprecations deprecations,
    SettingsDeprecatedPropertyBehavior deprecatedPropertyBehavior,
    ILogger log
)
{
    public RecyclarrSettings LoadAndOptionallyCreate()
    {
        var yamlPath = paths.ConfigDirectory.YamlFile("settings") ?? CreateDefaultSettingsFile();

        RecyclarrSettings settings;

        try
        {
            using var stream = yamlPath.OpenText();
            var deserializer = serializerFactory.CreateDeserializer(YamlFileType.Settings);
            settings =
                deserializer.Deserialize<RecyclarrSettings?>(stream.ReadToEnd())
                ?? new RecyclarrSettings();
        }
        catch (YamlException e)
        {
            // Same unwrap pattern as ConfigParser: extract layer-1 exceptions with file context
            if (e.FindInnerException<ConfigParsingException>() is { } inner)
            {
                throw new ConfigParsingException(inner.Message, (int)e.Start.Line, e)
                {
                    FilePath = yamlPath,
                };
            }

            throw;
        }

        // Report any deprecated properties that were silently skipped during parsing
        foreach (var message in deprecatedPropertyBehavior.Deprecations)
        {
            log.Warning("[DEPRECATED] {Message}", message);
        }

        // Apply deprecation transformations before validation
        settings = deprecations.CheckAndTransform(settings);

        ValidateSettings(settings);
        return settings;
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
