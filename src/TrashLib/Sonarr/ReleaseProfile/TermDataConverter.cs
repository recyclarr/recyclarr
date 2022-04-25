using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TrashLib.Sonarr.ReleaseProfile;

internal class TermDataConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        var token = JToken.Load(reader);
        return token.Type switch
        {
            JTokenType.Object => token.ToObject<TermData>(),
            JTokenType.String => new TermData {Term = token.ToString()},
            _ => null
        };
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(TermData);
    }
}
