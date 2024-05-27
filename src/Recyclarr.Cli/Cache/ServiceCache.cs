using System.IO.Abstractions;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Recyclarr.Common.Extensions;
using Recyclarr.Json;

namespace Recyclarr.Cli.Cache;

public partial class ServiceCache(ICacheStoragePath storagePath, ILogger log) : IServiceCache
{
    private readonly JsonSerializerOptions _jsonSettings = GlobalJsonSerializerSettings.Recyclarr;

    public T? Load<T>() where T : class
    {
        var path = PathFromAttribute<T>();
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
            log.Error(e, "Failed to read cache data, will proceed without cache");
        }

        return null;
    }

    public void Save<T>(T obj) where T : class
    {
        var path = PathFromAttribute<T>();
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

    private IFileInfo PathFromAttribute<T>()
    {
        var objectName = GetCacheObjectNameAttribute<T>();
        if (!AllowedObjectNameCharactersRegex().IsMatch(objectName))
        {
            throw new ArgumentException($"Object name '{objectName}' has unacceptable characters");
        }

        return storagePath.CalculatePath(objectName);
    }

    [GeneratedRegex(@"^[\w-]+$", RegexOptions.None, 1000)]
    private static partial Regex AllowedObjectNameCharactersRegex();
}
