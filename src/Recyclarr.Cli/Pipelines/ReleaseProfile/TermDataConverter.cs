using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Recyclarr.Cli.Pipelines.ReleaseProfile;

internal class TermDataConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        // Not to be used for serialization
        throw new NotImplementedException();
    }

    public override object? ReadJson(
        JsonReader reader,
        Type objectType,
        object? existingValue,
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
