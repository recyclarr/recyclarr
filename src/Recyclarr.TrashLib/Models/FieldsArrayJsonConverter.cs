using System.Text.Json;
using System.Text.Json.Serialization;

namespace Recyclarr.TrashLib.Models;

public class FieldsArrayJsonConverter : JsonConverter<object>
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(CustomFormatFieldData) ||
            Array.Exists(typeToConvert.GetInterfaces(),
                x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
    }

    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.StartObject)
        {
            return new[] {JsonSerializer.Deserialize<CustomFormatFieldData>(ref reader, options)!};
        }

        return JsonSerializer.Deserialize<CustomFormatFieldData[]>(ref reader, options)!;
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}
