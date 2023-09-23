using System.Text.Json;
using System.Text.Json.Serialization;
using Recyclarr.Json;

namespace Recyclarr.TrashGuide.ReleaseProfile;

public class TermDataConverter : JsonConverter<TermData>
{
    public override TermData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.String)
        {
            var str = reader.GetString();
            return str is not null ? new TermData {Term = str} : null;
        }

        return JsonSerializer.Deserialize<TermData>(ref reader, options.CopyOptionsWithout<TermDataConverter>());
    }

    public override void Write(Utf8JsonWriter writer, TermData value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options.CopyOptionsWithout<TermDataConverter>());
    }
}
