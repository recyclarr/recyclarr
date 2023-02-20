using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Recyclarr.Common.Extensions;

public static class JsonNetExtensions
{
    public static JEnumerable<T> Children<T>(this JToken token, string key)
        where T : JToken
    {
        return token[key]?.Children<T>() ?? JEnumerable<T>.Empty;
    }

    public static T ValueOrThrow<T>(this JToken token, string key)
        where T : class
    {
        var value = token.Value<T>(key);
        if (value is null)
        {
            throw new ArgumentNullException(token.Path);
        }

        return value;
    }

    public static T Clone<T>(this T source) where T : notnull
    {
        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source))
            ?? throw new ArgumentException("Could not deep clone", nameof(source));
    }
}
