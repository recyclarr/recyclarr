using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Config.Parsing.ErrorHandling;
using Recyclarr.TrashLib.Config.Yaml;
using Recyclarr.TrashLib.Startup;
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
        if (!_paths.SettingsPath.Exists)
        {
            CreateDefaultSettingsFile();
        }

        try
        {
            using var stream = _paths.SettingsPath.OpenText();
            var deserializer = serializerFactory.CreateDeserializer();
            return deserializer.Deserialize<SettingsValues?>(stream.ReadToEnd()) ?? new SettingsValues();
        }
        catch (YamlException e)
        {
            _log.Debug(e, "Exception while parsing settings file");

            var line = e.Start.Line;
            var msg = ContextualMessages.GetContextualErrorFromException(e);
            _log.Error("Exception while parsing settings.yml at line {Line}: {Msg}", line, msg);

            throw;
        }
    }

    private void CreateDefaultSettingsFile()
    {
        const string fileData =
            "# yaml-language-server: $schema=https://raw.githubusercontent.com/recyclarr/recyclarr/master/schemas/settings-schema.json\n" +
            "\n" +
            "# Edit this file to customize the behavior of Recyclarr beyond its defaults\n" +
            "# For the settings file reference guide, visit the link to the wiki below:\n" +
            "# https://recyclarr.dev/wiki/yaml/settings-reference/\n";

        _paths.SettingsPath.CreateParentDirectory();
        using var stream = _paths.SettingsPath.CreateText();
        stream.Write(fileData);
    }
}
