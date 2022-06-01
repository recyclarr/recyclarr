using System.Data.HashFunction.FNV;
using System.IO.Abstractions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;
using TrashLib.Config.Services;

namespace TrashLib.Cache;

public class ServiceCache : IServiceCache
{
    private static readonly Regex AllowedObjectNameCharacters = new(@"^[\w-]+$", RegexOptions.Compiled);
    private readonly IConfigurationProvider _configProvider;
    private readonly IFileSystem _fs;
    private readonly IFNV1a _hash;
    private readonly ICacheStoragePath _storagePath;
    private readonly JsonSerializerSettings _jsonSettings;

    public ServiceCache(
        IFileSystem fs,
        ICacheStoragePath storagePath,
        IConfigurationProvider configProvider,
        ILogger log)
    {
        _fs = fs;
        _storagePath = storagePath;
        _configProvider = configProvider;
        Log = log;
        _hash = FNV1aFactory.Instance.Create(FNVConfig.GetPredefinedConfig(32));
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

    public T? Load<T>() where T : class
    {
        var path = PathFromAttribute<T>();
        if (!_fs.File.Exists(path))
        {
            return null;
        }

        var json = _fs.File.ReadAllText(path);

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

    public void Save<T>(T obj) where T : class
    {
        var path = PathFromAttribute<T>();
        _fs.Directory.CreateDirectory(_fs.Path.GetDirectoryName(path));
        _fs.File.WriteAllText(path, JsonConvert.SerializeObject(obj, _jsonSettings));
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

    private string BuildServiceGuid()
    {
        return _hash.ComputeHash(Encoding.ASCII.GetBytes(_configProvider.ActiveConfiguration.BaseUrl))
            .AsHexString();
    }

    private string PathFromAttribute<T>()
    {
        var objectName = GetCacheObjectNameAttribute<T>();
        if (!AllowedObjectNameCharacters.IsMatch(objectName))
        {
            throw new ArgumentException($"Object name '{objectName}' has unacceptable characters");
        }

        return _fs.Path.Combine(_storagePath.Path, BuildServiceGuid(), objectName + ".json");
    }
}
