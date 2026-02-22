using Recyclarr.Config.Parsing.ErrorHandling;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Recyclarr.Yaml.YamlDotNet;

/// <summary>
/// Detects structural YAML mismatches where a scalar value is expected but a mapping or sequence
/// is provided. Throws a user-friendly ConfigParsingException instead of letting YamlDotNet
/// produce a cryptic internal error.
/// </summary>
internal sealed class ScalarMismatchDeserializer : INodeDeserializer
{
    private static readonly HashSet<Type> ScalarTypes =
    [
        typeof(string),
        typeof(bool),
        typeof(int),
        typeof(long),
        typeof(float),
        typeof(double),
        typeof(decimal),
    ];

    public bool Deserialize(
        IParser reader,
        Type expectedType,
        Func<IParser, Type, object?> nestedObjectDeserializer,
        out object? value,
        ObjectDeserializer rootDeserializer
    )
    {
        value = null;

        if (!IsScalarTarget(expectedType))
        {
            return false;
        }

        if (!reader.Accept<ParsingEvent>(out var evt))
        {
            return false;
        }

        var message = evt switch
        {
            MappingStart => "not key-value pairs",
            SequenceStart => "not a list",
            _ => null,
        };

        if (message is null)
        {
            return false;
        }

        throw new ConfigParsingException(
            $"Expected a plain value at line {evt.Start.Line}, {message}",
            (int)evt.Start.Line,
            new InvalidOperationException(
                $"YAML node mismatch where scalar type '{expectedType.Name}' was expected"
            )
        );
    }

    private static bool IsScalarTarget(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return ScalarTypes.Contains(underlying) || underlying.IsEnum;
    }
}
