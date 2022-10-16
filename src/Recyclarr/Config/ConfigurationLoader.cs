using System.IO.Abstractions;
using FluentValidation;
using TrashLib.Config;
using TrashLib.Config.Services;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Recyclarr.Config;

public class ConfigurationLoader<T> : IConfigurationLoader<T>
    where T : IServiceConfiguration
{
    private readonly IDeserializer _deserializer;
    private readonly IFileSystem _fileSystem;
    private readonly IValidator<T> _validator;

    public ConfigurationLoader(
        IFileSystem fileSystem,
        IYamlSerializerFactory yamlFactory,
        IValidator<T> validator)
    {
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

            var configs = _deserializer.Deserialize<List<T>?>(parser);
            if (configs == null)
            {
                parser.SkipThisAndNestedEvents();
                continue;
            }

            ValidateConfigs(configSection, configs, validConfigs);
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
