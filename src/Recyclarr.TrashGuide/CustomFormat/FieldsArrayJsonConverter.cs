using System.Text.Json;
using System.Text.Json.Serialization;

namespace Recyclarr.TrashGuide.CustomFormat;

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
        return reader.TokenType switch
        {
            JsonTokenType.StartObject => ConvertObjectToArray(ref reader, options),
            JsonTokenType.StartArray => JsonSerializer.Deserialize<CustomFormatFieldData[]>(ref reader, options)!,
            _ => throw new JsonException("Unexpected token type for CF fields")
        };
    }

    private static CustomFormatFieldData[] ConvertObjectToArray(
        ref Utf8JsonReader reader,
        JsonSerializerOptions options)
    {
        var valueOptions = new JsonSerializerOptions(options);
        valueOptions.Converters.Add(new NondeterministicValueConverter());
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(ref reader, options)!
            .Select(x => new CustomFormatFieldData
            {
                Name = x.Key,
                Value = x.Value.Deserialize<object>(valueOptions)
            })
            .ToArray();
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}
