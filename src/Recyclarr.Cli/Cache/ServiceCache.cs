using System.IO.Abstractions;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Recyclarr.Common.Extensions;
using Recyclarr.Config.Models;
using Recyclarr.Json;

namespace Recyclarr.Cli.Cache;

public partial class ServiceCache(ICacheStoragePath storagePath, ILogger log) : IServiceCache
{
    private readonly JsonSerializerOptions _jsonSettings = GlobalJsonSerializerSettings.Recyclarr;

    public T? Load<T>(IServiceConfiguration config) where T : class
    {
        var path = PathFromAttribute<T>(config);
        log.Debug("Loading cache from path: {Path}", path.FullName);
        if (!path.Exists)
        {
            log.Debug("Cache path does not exist");
            return null;
        }

        try
        {
            using var stream = path.OpenRead();
            return JsonSerializer.Deserialize<T>(stream, _jsonSettings);
        }
        catch (JsonException e)
        {
            log.Error("Failed to read cache data, will proceed without cache. Reason: {Msg}", e.Message);
        }

        return null;
    }

    public void Save<T>(T obj, IServiceConfiguration config) where T : class
    {
        var path = PathFromAttribute<T>(config);
        log.Debug("Saving cache to path: {Path}", path.FullName);
        path.CreateParentDirectory();

        using var stream = path.Create();
        JsonSerializer.Serialize(stream, obj, _jsonSettings);
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

        return storagePath.CalculatePath(config, objectName);
    }

    [GeneratedRegex(@"^[\w-]+$", RegexOptions.None, 1000)]
    private static partial Regex AllowedObjectNameCharactersRegex();
}
