using System.Text.Json;
using System.Text.Json.Serialization;

namespace Recyclarr.Json;

public static class JsonExtensions
{
    public static JsonSerializerOptions CopyOptionsWithout<T>(this JsonSerializerOptions options)
        where T : JsonConverter
    {
        var jsonSerializerOptions = new JsonSerializerOptions(options);

        var jsonConverter = jsonSerializerOptions.Converters.FirstOrDefault(x =>
            x.GetType() == typeof(T)
        );
        if (jsonConverter is not null)
        {
            jsonSerializerOptions.Converters.Remove(jsonConverter);
        }

        return jsonSerializerOptions;
    }
}
