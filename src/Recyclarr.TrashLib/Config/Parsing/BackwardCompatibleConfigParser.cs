using AutoMapper;
using FluentValidation;
using Recyclarr.Config.Data.V1;
using Recyclarr.TrashLib.Config.Yaml;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Recyclarr.TrashLib.Config.Parsing;

using RootConfigYamlV1 = RootConfigYaml;

public class BackwardCompatibleConfigParser
{
    private readonly ILogger _log;
    private readonly IMapper _mapper;
    private readonly ConfigValidationExecutor _validator;
    private readonly IDeserializer _deserializer;

    // Order matters here. Types are mapped from top to bottom (front to back).
    // Newer types should be added to the top/start of the list.
    private readonly IReadOnlyList<Type> _configTypes = new[]
    {
        typeof(RootConfigYamlLatest),
        typeof(RootConfigYamlV1)
    };

    public BackwardCompatibleConfigParser(
        ILogger log,
        IYamlSerializerFactory yamlFactory,
        IMapper mapper,
        ConfigValidationExecutor validator)
    {
        _log = log;
        _mapper = mapper;
        _validator = validator;
        _deserializer = yamlFactory.CreateDeserializer();
    }

    private (int Index, object? Data) TryParseConfig(Func<TextReader> streamFactory)
    {
        Exception? firstException = null;

        // step 1: Iterate from NEWEST -> OLDEST until we successfully deserialize
        for (var i = 0; i < _configTypes.Count; ++i)
        {
            var configType = _configTypes[i];
            _log.Debug("Attempting deserialization using config type: {Type}", configType);

            try
            {
                using var stream = streamFactory();
                return (i, _deserializer.Deserialize(stream, configType));
            }
            catch (YamlException e)
            {
                _log.Debug(e.InnerException, "Exception during deserialization");
                firstException ??= e;
                // Ignore this exception and continue; we should continue to try older types
            }
        }

        throw firstException ?? new InvalidOperationException("Parsing failed for unknown reason");
    }

    private RootConfigYamlLatest MapConfigDataToLatest(int index, object data)
    {
        var currentType = _configTypes[index];

        // step 2: Using the same index, now go the other direction: OLDEST -> NEWEST, using IMapper to map
        // all the way up to the latest
        foreach (var nextType in _configTypes.Slice(0, index).Reverse())
        {
            if (!_validator.Validate(data))
            {
                throw new ValidationException($"Validation Failed for type: {data.GetType().Name}");
            }

            // If any mapping fails, the whole chain fails. Let the exception leak out and get handled outside.
            data = _mapper.Map(data, currentType, nextType);
            currentType = nextType;
        }

        return (RootConfigYamlLatest) data;
    }

    public RootConfigYamlLatest? ParseYamlConfig(Func<TextReader> streamFactory)
    {
        var (index, data) = TryParseConfig(streamFactory);
        return data is null ? null : MapConfigDataToLatest(index, data);
    }
}
