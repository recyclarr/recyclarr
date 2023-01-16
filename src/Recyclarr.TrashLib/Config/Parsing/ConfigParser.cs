using JetBrains.Annotations;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Config.Yaml;
using Recyclarr.TrashLib.Services.Radarr.Config;
using Recyclarr.TrashLib.Services.Sonarr.Config;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Recyclarr.TrashLib.Config.Parsing;

[UsedImplicitly]
public class ConfigParser
{
    private readonly ConfigValidationExecutor _validator;
    private readonly IDeserializer _deserializer;
    private readonly ConfigRegistry _configs = new();
    private SupportedServices? _currentSection;

    private readonly Dictionary<SupportedServices, Type> _configTypes = new()
    {
        {SupportedServices.Sonarr, typeof(SonarrConfiguration)},
        {SupportedServices.Radarr, typeof(RadarrConfiguration)}
    };

    public IConfigRegistry Configs => _configs;

    public ConfigParser(
        IYamlSerializerFactory yamlFactory,
        ConfigValidationExecutor validator)
    {
        _validator = validator;
        _deserializer = yamlFactory.CreateDeserializer();
    }

    public bool SetCurrentSection(string name)
    {
        if (!Enum.TryParse(name, true, out SupportedServices key) || !_configTypes.ContainsKey(key))
        {
            return false;
        }

        _currentSection = key;
        return true;
    }

    public void ParseAndAddConfig(Parser parser)
    {
        var lineNumber = parser.Current?.Start.Line;

        string? instanceName = null;
        if (parser.TryConsume<Scalar>(out var key))
        {
            instanceName = key.Value;
        }

        if (_currentSection is null)
        {
            throw new YamlException("SetCurrentSection() must be set before parsing");
        }

        var configType = _configTypes[_currentSection.Value];
        var newConfig = (ServiceConfiguration?) _deserializer.Deserialize(parser, configType);
        if (newConfig is null)
        {
            throw new YamlException(
                $"Unable to deserialize instance at line {lineNumber} using configuration type {_currentSection}");
        }

        newConfig.InstanceName = instanceName;
        newConfig.LineNumber = lineNumber ?? 0;

        if (!_validator.Validate(newConfig))
        {
            throw new YamlException("Validation failed");
        }

        _configs.Add(newConfig);
    }
}
