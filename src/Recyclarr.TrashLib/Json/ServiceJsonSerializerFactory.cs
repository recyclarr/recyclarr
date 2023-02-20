using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Recyclarr.TrashLib.Json;

public static class ServiceJsonSerializerFactory
{
    public static JsonSerializerSettings Settings { get; } = new()
    {
        // This makes sure that null properties, such as maxSize and preferredSize in Radarr
        // Quality Definitions, do not get written out to JSON request bodies.
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = new ServiceContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        }
    };

    public static JsonSerializer Create()
    {
        return JsonSerializer.Create(Settings);
    }
}
