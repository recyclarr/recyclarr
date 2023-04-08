using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Recyclarr.TrashLib.Pipelines.CustomFormat.Models;

public class FieldsArrayJsonConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }

    public override object? ReadJson(
        JsonReader reader,
        Type objectType,
        object? existingValue,
        JsonSerializer serializer)
    {
        var token = JToken.Load(reader);

        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        return token.Type switch
        {
            JTokenType.Object => new[] {token.ToObject<CustomFormatFieldData>()},
            JTokenType.Array => token.ToObject<CustomFormatFieldData[]>(),
            _ => throw new InvalidOperationException("Unsupported token type for CustomFormatFieldData")
        };
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType.IsArray;
    }
}
