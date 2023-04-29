using System.IO.Abstractions;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Interfaces;

namespace Recyclarr.TrashLib.Cache;

public partial class ServiceCache : IServiceCache
{
    private readonly ICacheStoragePath _storagePath;
    private readonly JsonSerializerSettings _jsonSettings;

    public ServiceCache(ICacheStoragePath storagePath, ILogger log)
    {
        _storagePath = storagePath;
        Log = log;
        _jsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            }
        };
    }

    private ILogger Log { get; }

    public T? Load<T>(IServiceConfiguration config) where T : class
    {
        var path = PathFromAttribute<T>(config);
        if (!path.Exists)
        {
            return null;
        }

        using var stream = path.OpenText();
        var json = stream.ReadToEnd();

        try
        {
            return JsonConvert.DeserializeObject<T>(json, _jsonSettings);
        }
        catch (JsonException e)
        {
            Log.Error("Failed to read cache data, will proceed without cache. Reason: {Msg}", e.Message);
        }

        return null;
    }

    public void Save<T>(T obj, IServiceConfiguration config) where T : class
    {
        var path = PathFromAttribute<T>(config);
        path.CreateParentDirectory();

        var serializer = JsonSerializer.Create(_jsonSettings);

        using var stream = new JsonTextWriter(path.CreateText());
        serializer.Serialize(stream, obj);
    }

    private static string GetCacheObjectNameAttribute<T>()
    {
        var attribute = typeof(T).GetCustomAttribute<CacheObjectNameAttribute>();
        if (attribute == null)
        {
            throw new ArgumentException($"{nameof(CacheObjectNameAttribute)} is missing on type {nameof(T)}");
        }

        return attribute.Name;
    }

    private IFileInfo PathFromAttribute<T>(IServiceConfiguration config)
    {
        var objectName = GetCacheObjectNameAttribute<T>();
        if (!AllowedObjectNameCharactersRegex().IsMatch(objectName))
        {
            throw new ArgumentException($"Object name '{objectName}' has unacceptable characters");
        }

        return _storagePath.CalculatePath(config, objectName);
    }

    [GeneratedRegex("^[\\w-]+$", RegexOptions.None, 1000)]
    private static partial Regex AllowedObjectNameCharactersRegex();
}
