using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Recyclarr.TrashLib.Json;

public static class GlobalJsonSerializerSettings
{
    /// <summary>
    /// JSON settings used for starr service API payloads.
    /// </summary>
    public static JsonSerializerSettings Services { get; } = new()
    {
        // This makes sure that null properties, such as maxSize and preferredSize in Radarr
        // Quality Definitions, do not get written out to JSON request bodies.
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = new ServiceContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        }
    };

    /// <summary>
    /// JSON settings used by cache and other Recyclarr-owned JSON files.
    /// </summary>
    public static JsonSerializerSettings Recyclarr { get; } = new()
    {
        Formatting = Formatting.Indented,
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        }
    };

    /// <summary>
    /// JSON settings used by Trash Guides JSON files.
    /// </summary>
    public static JsonSerializerSettings Guide => Recyclarr;
}
