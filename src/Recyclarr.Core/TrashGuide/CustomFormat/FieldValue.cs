using System.Text.Json;
using System.Text.Json.Serialization;

namespace Recyclarr.TrashGuide.CustomFormat;

/// <summary>
/// Strongly-typed wrapper for custom format field values. Normalizes values from different
/// deserialization paths (System.Text.Json JsonElement vs CLR primitives) so that equality
/// comparisons work correctly regardless of source.
/// </summary>
[JsonConverter(typeof(Converter))]
public readonly record struct FieldValue(object? Inner)
{
    public static FieldValue From(object? value)
    {
        return new FieldValue(Normalize(value));
    }

    public bool Equals(FieldValue other) => Equals(Inner, other.Inner);

    public override int GetHashCode() => Inner?.GetHashCode() ?? 0;

    private static object? Normalize(object? value)
    {
        return value switch
        {
            null => null,
            string s => s,
            int i => i,
            double d => d,
            bool b => b,
            JsonElement e => UnwrapJsonElement(e),
            _ => throw new ArgumentException($"Unsupported field value type: {value.GetType()}"),
        };
    }

    private static object? UnwrapJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt32(out var i) => i,
            JsonValueKind.Number when element.TryGetDouble(out var d) => d,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => throw new JsonException(
                $"Unsupported JSON value kind for CF field: {element.ValueKind}"
            ),
        };
    }

    private sealed class Converter : JsonConverter<FieldValue>
    {
        public override FieldValue Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            var element = JsonSerializer.Deserialize<JsonElement>(ref reader);
            return From(element);
        }

        public override void Write(
            Utf8JsonWriter writer,
            FieldValue value,
            JsonSerializerOptions options
        )
        {
            switch (value.Inner)
            {
                case null:
                    writer.WriteNullValue();
                    break;
                case string s:
                    writer.WriteStringValue(s);
                    break;
                case int i:
                    writer.WriteNumberValue(i);
                    break;
                case double d:
                    writer.WriteNumberValue(d);
                    break;
                case bool b:
                    writer.WriteBooleanValue(b);
                    break;
                default:
                    throw new JsonException(
                        $"Serialization of type {value.Inner.GetType()} is not supported"
                    );
            }
        }
    }
}
