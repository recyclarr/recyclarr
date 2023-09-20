using System.Text.Json.Serialization.Metadata;

namespace Recyclarr.Json;

[AttributeUsage(AttributeTargets.Property)]
public sealed class JsonNoSerializeAttribute : Attribute
{
}

public static class JsonSerializationModifiers
{
    private static bool HasAttribute<T>(JsonPropertyInfo? prop, IReadOnlyDictionary<string, IEnumerable<Type>> allAttrs)
        where T : Attribute
    {
        if (prop is null)
        {
            return false;
        }

        if (!allAttrs.TryGetValue(prop.Name, out var attrs))
        {
            return false;
        }

        return attrs.Any(x => x.IsAssignableTo(typeof(T)));
    }

    public static void IgnoreNoSerializeAttribute(JsonTypeInfo type)
    {
        var attrs = type.Properties
            .Select(x => (Property: x, Attributes: x.PropertyType.GetCustomAttributes(false).Select(y => y.GetType())))
            .Where(x => x.Attributes.Any())
            .ToDictionary(x => x.Property.Name, x => x.Attributes);

        var props = type.Properties;
        foreach (var prop in props)
        {
            prop.ShouldSerialize = (_, _) => !HasAttribute<JsonNoSerializeAttribute>(prop, attrs);
        }
    }
}
