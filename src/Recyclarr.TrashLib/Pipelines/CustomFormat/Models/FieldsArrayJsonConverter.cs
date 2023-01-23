using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Recyclarr.TrashLib.Pipelines.CustomFormat.Models;

public class FieldsArrayJsonConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is not CustomFormatFieldData)
        {
            serializer.Serialize(writer, value);
            return;
        }

        var token = JToken.FromObject(value);
        if (token.Type == JTokenType.Object)
        {
            var array = new JArray(token);
            serializer.Serialize(writer, array);
        }
        else
        {
            serializer.Serialize(writer, token);
        }
    }

    public override object? ReadJson(
        JsonReader reader,
        Type objectType,
        object? existingValue,
        JsonSerializer serializer)
    {
        if (existingValue is not CustomFormatFieldData)
        {
            return new CustomFormatFieldData();
        }

        var token = JToken.Load(reader);
        return token.Type switch
        {
            JTokenType.Object => token.ToObject<CustomFormatFieldData>(),
            JTokenType.Array => token.ToObject<CustomFormatFieldData[]>()?.SingleOrDefault(),
            _ => null
        };
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType.IsArray;
    }
}
