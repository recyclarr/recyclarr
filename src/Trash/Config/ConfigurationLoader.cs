using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using FluentValidation;
using TrashLib.Config;
using TrashLib.Config.Services;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Trash.Config;

public class ConfigurationLoader<T> : IConfigurationLoader<T>
    where T : IServiceConfiguration
{
    private readonly IConfigurationProvider _configProvider;
    private readonly IDeserializer _deserializer;
    private readonly IFileSystem _fileSystem;
    private readonly IValidator<T> _validator;

    public ConfigurationLoader(
        IConfigurationProvider configProvider,
        IFileSystem fileSystem,
        IYamlDeserializerFactory yamlFactory,
        IValidator<T> validator)
    {
        _configProvider = configProvider;
        _fileSystem = fileSystem;
        _validator = validator;
        _deserializer = yamlFactory.Create();
    }

    public IEnumerable<T> Load(string propertyName, string configSection)
    {
        using var stream = _fileSystem.File.OpenText(propertyName);
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

            var configs = _deserializer.Deserialize<List<T>?>(parser);
            if (configs == null)
            {
                parser.SkipThisAndNestedEvents();
                continue;
            }

            ValidateConfigs(configSection, configs, validConfigs);
            parser.SkipThisAndNestedEvents();
        }

        if (validConfigs.Count == 0)
        {
            throw new ConfigurationException(configSection, typeof(T), "There are no configured instances defined");
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
        foreach (var config in configFiles.SelectMany(file => Load(file, configSection)))
        {
            _configProvider.ActiveConfiguration = config;
            yield return config;
        }
    }
}
