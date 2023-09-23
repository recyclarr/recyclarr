using System.Text.Json;
using System.Text.Json.Serialization;

namespace Recyclarr.TrashGuide.CustomFormat;

public class FieldValueConverter : JsonConverter<object>
{
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetInt32(),
            JsonTokenType.String => reader.GetString(),
            _ => throw new JsonException($"CF field of type {reader.TokenType} is not supported")
        };
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}
