using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Recyclarr.Server;

/// <summary>
/// Shortens the OpenAPI schema IDs of polymorphic derived types. ASP.NET Core names a derived
/// schema by joining the base and derived IDs, so a derived type that repeats its base name
/// (<c>PlanningOutcomeResponse</c> + <c>...PlanningOutcomeResponse</c>) produces a doubled name in
/// the spec and in every generated client. Dropping the ending a derived name shares with its base
/// name removes that repetition; the base name still prefixes the final schema ID.
/// </summary>
internal static class PolymorphicSchemaIds
{
    public static Func<JsonTypeInfo, string?> Wrap(Func<JsonTypeInfo, string?> inner) =>
        typeInfo => Shorten(typeInfo.Type, inner(typeInfo));

    private static string? Shorten(Type type, string? id)
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
