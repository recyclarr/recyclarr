using System.Text.Json;
using System.Text.Json.Serialization;

namespace Recyclarr.Json;

public class CollectionJsonConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return !IsExcludedType(typeToConvert)
            && typeToConvert.IsGenericType
            && IsAssignableToGenericType(typeToConvert, typeof(IEnumerable<>));
    }

    private static bool IsExcludedType(Type type)
    {
        return type.IsPrimitive || type == typeof(string);
    }

    private static bool IsAssignableToGenericType(Type givenType, Type genericType)
    {
        var interfaceTypes = givenType
            .GetInterfaces()
            .Where(x => x.IsGenericType)
            .Select(x => x.GetGenericTypeDefinition());

        return interfaceTypes.Contains(genericType);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var elementType = typeToConvert.GetGenericArguments()[0];
        Type converterType;

        if (typeToConvert.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>))
        {
            converterType = typeof(ReadOnlyCollectionJsonConverter<>).MakeGenericType(elementType);
        }
        else
        {
            throw new JsonException();
        }

        var instance = Activator.CreateInstance(converterType) ?? throw new JsonException();

        return (JsonConverter)instance;
    }
}
