using System.IO.Abstractions;
using FluentValidation;
using Serilog;
using TrashLib.Config;
using TrashLib.Config.Services;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Recyclarr.Config;

public class ConfigurationLoader<T> : IConfigurationLoader<T>
    where T : ServiceConfiguration
{
    private readonly ILogger _log;
    private readonly IDeserializer _deserializer;
    private readonly IFileSystem _fileSystem;
    private readonly IValidator<T> _validator;

    public ConfigurationLoader(
        ILogger log,
        IFileSystem fileSystem,
        IYamlSerializerFactory yamlFactory,
        IValidator<T> validator)
    {
        _log = log;
        _fileSystem = fileSystem;
        _validator = validator;
        _deserializer = yamlFactory.CreateDeserializer();
    }

    public IEnumerable<T> Load(string file, string configSection)
    {
        using var stream = _fileSystem.File.OpenText(file);
        return LoadFromStream(stream, configSection);
    }

    public IEnumerable<T> LoadFromStream(TextReader stream, string configSection)
    {
        var parser = new Parser(stream);
        parser.Consume<StreamStart>();
        parser.Consume<DocumentStart>();
        parser.Consume<MappingStart>();

        var validConfigs = new List<T>();
        while (parser.TryConsume<Scalar>(out var key))
        {
            if (key.Value != configSection)
            {
                parser.SkipThisAndNestedEvents();
                continue;
            }

            List<T>? configs;
            switch (parser.Current)
            {
                case MappingStart:
                    configs = _deserializer.Deserialize<Dictionary<string, T>>(parser)
                        .Select(kvp =>
                        {
                            kvp.Value.Name = kvp.Key;
                            return kvp.Value;
                        })
                        .ToList();
                    break;

                case SequenceStart:
                    _log.Warning(
                        "Found array-style list of instances instead of named-style. Array-style lists of Sonarr/Radarr " +
                        "instances are deprecated");
                    configs = _deserializer.Deserialize<List<T>>(parser);
                    break;

                default:
                    configs = null;
                    break;
            }

            if (configs is not null)
            {
                ValidateConfigs(configSection, configs, validConfigs);
            }

            parser.SkipThisAndNestedEvents();
        }

        return validConfigs;
    }

    private void ValidateConfigs(string configSection, IEnumerable<T> configs, ICollection<T> validConfigs)
    {
        foreach (var config in configs)
        {
            var result = _validator.Validate(config);
            if (result is {IsValid: false})
            {
                throw new ConfigurationException(configSection, typeof(T), result.Errors);
            }

            validConfigs.Add(config);
        }
    }

    public IEnumerable<T> LoadMany(IEnumerable<string> configFiles, string configSection)
    {
        return configFiles.SelectMany(file => Load(file, configSection));
    }
}
