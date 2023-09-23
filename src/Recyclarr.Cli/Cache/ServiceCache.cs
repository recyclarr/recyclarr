using System.IO.Abstractions;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Recyclarr.Common.Extensions;
using Recyclarr.Config.Models;
using Recyclarr.Json;
using Recyclarr.TrashLib.Interfaces;

namespace Recyclarr.Cli.Cache;

public partial class ServiceCache : IServiceCache
{
    private readonly ICacheStoragePath _storagePath;
    private readonly JsonSerializerOptions _jsonSettings;
    private readonly ILogger _log;

    public ServiceCache(ICacheStoragePath storagePath, ILogger log)
    {
        _storagePath = storagePath;
        _log = log;
        _jsonSettings = GlobalJsonSerializerSettings.Recyclarr;
    }

    public T? Load<T>(IServiceConfiguration config) where T : class
    {
        var path = PathFromAttribute<T>(config);
        _log.Debug("Loading cache from path: {Path}", path.FullName);
        if (!path.Exists)
        {
            _log.Debug("Cache path does not exist");
            return null;
        }

        using var stream = path.OpenText();
        var json = stream.ReadToEnd();

        try
        {
            return JsonSerializer.Deserialize<T>(json, _jsonSettings);
        }
        catch (JsonException e)
        {
            _log.Error("Failed to read cache data, will proceed without cache. Reason: {Msg}", e.Message);
        }

        return null;
    }

    public void Save<T>(T obj, IServiceConfiguration config) where T : class
    {
        var path = PathFromAttribute<T>(config);
        _log.Debug("Saving cache to path: {Path}", path.FullName);
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

        return _storagePath.CalculatePath(config, objectName);
    }

    [GeneratedRegex(@"^[\w-]+$", RegexOptions.None, 1000)]
    private static partial Regex AllowedObjectNameCharactersRegex();
}
