using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Recyclarr.Json;

public static class GlobalJsonSerializerSettings
{
    /// <summary>
    /// JSON settings used for starr service API payloads.
    /// </summary>
    public static JsonSerializerOptions Services { get; } =
        new()
        {
            // This makes sure that null properties, such as maxSize and preferredSize in Radarr
            // Quality Definitions, do not get written out to JSON request bodies.
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { JsonSerializationModifiers.IgnoreNoSerializeAttribute },
            },
        };

    /// <summary>
    /// JSON settings used by state and other Recyclarr-owned JSON files.
    /// </summary>
    public static JsonSerializerOptions Recyclarr { get; } =
        new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, WriteIndented = true };

    /// <summary>
    /// JSON settings used by Trash Guides JSON files.
    /// </summary>
    public static JsonSerializerOptions Guide { get; } =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
        };

    /// <summary>
    /// JSON settings used by Trash Guides metadata files.
    /// </summary>
    public static JsonSerializerOptions Metadata { get; } =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
        };
}
