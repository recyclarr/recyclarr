using System.Text.Json;
using System.Text.Json.Serialization;

namespace Recyclarr.Json;

[UsedImplicitly]
public sealed class ReadOnlyCollectionJsonConverter<TElement>
    : JsonConverter<IReadOnlyCollection<TElement>>
{
    public override IReadOnlyCollection<TElement> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException();
        }

        var list = new List<TElement>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            var elementValue = JsonSerializer.Deserialize<TElement>(ref reader, options);
            if (elementValue is not null)
            {
                list.Add(elementValue);
            }
        }

        return list;
    }

    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyCollection<TElement> value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartArray();
        foreach (var element in value)
        {
            JsonSerializer.Serialize(writer, element, options);
        }

        writer.WriteEndArray();
    }
}
