using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Common.YamlDotNet;
using FluentValidation;
using TrashLib.Config;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Trash.Config
{
    public class ConfigurationLoader<T> : IConfigurationLoader<T>
        where T : IServiceConfiguration
    {
        private readonly IConfigProvider<T> _configProvider;
        private readonly IDeserializer _deserializer;
        private readonly IFileSystem _fileSystem;
        private readonly IValidator<T> _validator;

        public ConfigurationLoader(
            IConfigProvider<T> configProvider,
            IFileSystem fileSystem,
            IObjectFactory objectFactory,
            IValidator<T> validator)
        {
            _configProvider = configProvider;
            _fileSystem = fileSystem;
            _validator = validator;
            _deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .WithTypeConverter(new YamlNullableEnumTypeConverter())
                .WithObjectFactory(objectFactory)
                .Build();
        }

        public IEnumerable<T> Load(string configPath, string configSection)
        {
            using var stream = _fileSystem.File.OpenText(configPath);
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
                if (key.Value == configSection)
                {
                    var configs = (List<T>?) _deserializer.Deserialize<List<T>>(parser);
                    if (configs != null)
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
                }

                parser.SkipThisAndNestedEvents();
            }

            if (validConfigs.Count == 0)
            {
                throw new ConfigurationException(configSection, typeof(T), "There are no configured instances defined");
            }

            return validConfigs;
        }

        public IEnumerable<T> LoadMany(IEnumerable<string> configFiles, string configSection)
        {
            foreach (var config in configFiles.SelectMany(file => Load(file, configSection)))
            {
                _configProvider.Active = config;
                yield return config;
            }
        }
    }
}
