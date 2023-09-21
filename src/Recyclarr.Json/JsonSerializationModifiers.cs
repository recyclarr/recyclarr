using System.Text.Json.Serialization.Metadata;

namespace Recyclarr.Json;

[AttributeUsage(AttributeTargets.Property)]
public sealed class JsonNoSerializeAttribute : Attribute
{
}

public static class JsonSerializationModifiers
{
    public static void IgnoreNoSerializeAttribute(JsonTypeInfo type)
    {
        var propertiesToRemove = type.Properties
            .Where(x => x.AttributeProvider?.IsDefined(typeof(JsonNoSerializeAttribute), false) ?? false)
            .ToList();

        foreach (var prop in propertiesToRemove)
        {
            prop.ShouldSerialize = (_, _) => false;
        }
    }
}
