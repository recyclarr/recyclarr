using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Recyclarr.Server.Api.OpenApi;

/// <summary>
/// Names the component schemas of the OpenAPI document. Schema IDs become type names in generated
/// clients, so they must describe the wire shape, not C# implementation choices. This adjusts the
/// FastEndpoints naming, which names every type after its C# declaration:
/// <list type="bullet">
/// <item>
/// Collections and dictionaries get no component schema; they appear inline as arrays and maps
/// where used. Otherwise each gets a component named after its C# type
/// (<c>IReadOnlyListOfFoo</c>), and changing a property from one collection type to another would
/// rename a client type.
/// </item>
/// <item>
/// Generic types name a collection type argument after its elements
/// (<c>FooResponseOfBarArray</c>, not <c>FooResponseOfIReadOnlyListOfBar</c>).
/// </item>
/// <item>
/// Polymorphic derived types drop the ending their name shares with their base name. ASP.NET Core
/// names a derived schema by joining the base and derived IDs, so a derived type that repeats its
/// base name (<c>PlanningOutcomeResponse</c> + <c>...PlanningOutcomeResponse</c>) would produce a
/// doubled name.
/// </item>
/// </list>
/// </summary>
internal static class SchemaReferenceIds
{
    public static Func<JsonTypeInfo, string?> Wrap(Func<JsonTypeInfo, string?> inner) =>
        typeInfo =>
        {
            if (typeInfo.Kind is JsonTypeInfoKind.Enumerable or JsonTypeInfoKind.Dictionary)
            {
                return null;
            }

            var type = typeInfo.Type;
            var id =
                type.IsGenericType && Nullable.GetUnderlyingType(type) is null
                    ? GenericName(type, typeInfo.Options)
                    : inner(typeInfo);

            return ShortenDerived(type, id);
        };

    private static string GenericName(Type type, JsonSerializerOptions options)
    {
        if (
            options.GetTypeInfo(type) is
            { Kind: JsonTypeInfoKind.Enumerable, ElementType: { } item }
        )
        {
            return GenericName(item, options) + "Array";
        }

        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var arguments = type.GetGenericArguments().Select(x => GenericName(x, options));
        return $"{type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)]}Of{string.Join("And", arguments)}";
    }

    private static string? ShortenDerived(Type type, string? id)
    {
        var baseType = type.BaseType;
        if (id is null || baseType?.GetCustomAttribute<JsonPolymorphicAttribute>() is null)
        {
            return id;
        }

        var shared = SharedSuffixLength(id, baseType.Name);
        return shared < id.Length ? id[..^shared] : id;
    }

    private static int SharedSuffixLength(string a, string b)
    {
        var length = 0;
        while (length < a.Length && length < b.Length && a[^(length + 1)] == b[^(length + 1)])
        {
            length++;
        }

        return length;
    }
}
