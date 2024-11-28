using System.Text.Json;
using System.Text.Json.Serialization;

namespace Recyclarr.TrashGuide.CustomFormat;

public class NondeterministicValueConverter : JsonConverter<object>
{
    public override object? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number when reader.TryGetInt32(out var value) => value,
            JsonTokenType.Number when reader.TryGetDouble(out var value) => value,
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Null => null,
            _ => throw new JsonException($"CF field of type {reader.TokenType} is not supported"),
        };
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        try
        {
            switch (value)
            {
                case null:
                    writer.WriteNullValue();
                    break;

                case string strValue:
                    writer.WriteStringValue(strValue);
                    break;

                case int intValue:
                    writer.WriteNumberValue(intValue);
                    break;

                case double doubleValue:
                    writer.WriteNumberValue(doubleValue);
                    break;

                case bool boolValue:
                    writer.WriteBooleanValue(boolValue);
                    break;

                default:
                    throw new JsonException(
                        $"Serialization of type {value.GetType()} is not supported"
                    );
            }
        }
        catch (Exception ex)
        {
            throw new JsonException(
                $"Serialization failed for value of type {value?.GetType()}",
                ex
            );
        }
    }
}
