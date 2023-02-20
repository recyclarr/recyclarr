using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Recyclarr.TrashLib.Json;

[AttributeUsage(AttributeTargets.Property)]
public sealed class JsonNoSerializeAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class JsonNoDeserializeAttribute : Attribute
{
}

public class ServiceContractResolver : DefaultContractResolver
{
    private static bool HasAttribute<T>(JsonProperty prop, Dictionary<string, IEnumerable<Type>> allAttrs)
        where T : Attribute
    {
        var name = prop.UnderlyingName;
        if (name is null)
        {
            return false;
        }

        if (!allAttrs.TryGetValue(name, out var attrs))
        {
            return false;
        }

        return attrs.Any(x => x.IsAssignableTo(typeof(T)));
    }

    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        var attrs = type.GetProperties()
            .Select(x => (Property: x, Attributes: x.GetCustomAttributes(false).Select(y => y.GetType())))
            .Where(x => x.Attributes.Any())
            .ToDictionary(x => x.Property.Name, x => x.Attributes);

        var props = base.CreateProperties(type, memberSerialization);
        foreach (var prop in props)
        {
            prop.ShouldSerialize = _ => !HasAttribute<JsonNoSerializeAttribute>(prop, attrs);
            prop.ShouldDeserialize = _ => !HasAttribute<JsonNoDeserializeAttribute>(prop, attrs);
        }

        return props;
    }
}
