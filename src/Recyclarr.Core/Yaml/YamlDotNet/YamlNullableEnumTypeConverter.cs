using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Recyclarr.Yaml.YamlDotNet;

// A workaround for nullable enums in YamlDotNet taken from:
// https://github.com/aaubry/YamlDotNet/issues/544#issuecomment-778062351
public class YamlNullableEnumTypeConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return Nullable.GetUnderlyingType(type)?.IsEnum ?? false;
    }

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        type =
            Nullable.GetUnderlyingType(type)
            ?? throw new ArgumentException("Expected nullable enum type for ReadYaml");

        if (parser.Accept<NodeEvent>(out var @event) && NodeIsNull(@event))
        {
            parser.SkipThisAndNestedEvents();
            return null;
        }

        var scalar = parser.Consume<Scalar>();
        try
        {
            return Enum.Parse(type, scalar.Value, true);
        }
        catch (Exception ex)
        {
            var validValues = FormatValidValues(type);
            throw new YamlException(
                scalar.Start,
                scalar.End,
                $"Invalid value '{scalar.Value}'. Valid options: {validValues}",
                ex
            );
        }
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        type =
            Nullable.GetUnderlyingType(type)
            ?? throw new ArgumentException("Expected nullable enum type for WriteYaml");

        if (value == null)
        {
            return;
        }

        var toWrite =
            Enum.GetName(type, value)
            ?? throw new InvalidOperationException($"Invalid value {value} for enum: {type}");
        emitter.Emit(new Scalar(null!, null!, toWrite, ScalarStyle.Any, true, false));
    }

    // Formats enum values as YAML snake_case for user-facing messages
    private static string FormatValidValues(Type enumType)
    {
        var naming = UnderscoredNamingConvention.Instance;
        var values = Enum.GetNames(enumType).Select(n => naming.Apply(n));
        return string.Join(", ", values);
    }

    private static bool NodeIsNull(NodeEvent nodeEvent)
    {
        // http://yaml.org/type/null.html

        if (nodeEvent.Tag == "tag:yaml.org,2002:null")
        {
            return true;
        }

        if (nodeEvent is not Scalar { Style: ScalarStyle.Plain } scalar)
        {
            return false;
        }

        var value = scalar.Value;
        return value is "" or "~" or "null" or "Null" or "NULL";
    }
}
