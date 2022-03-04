using System.Data.HashFunction.FNV;
using System.IO.Abstractions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Serilog;
using TrashLib.Config.Services;

namespace TrashLib.Cache;

internal class ServiceCache : IServiceCache
{
    private static readonly Regex AllowedObjectNameCharacters = new(@"^[\w-]+$", RegexOptions.Compiled);
    private readonly IConfigurationProvider _configProvider;
    private readonly IFileSystem _fileSystem;
    private readonly IFNV1a _hash;
    private readonly ICacheStoragePath _storagePath;

    public ServiceCache(IFileSystem fileSystem, ICacheStoragePath storagePath,
        IConfigurationProvider configProvider,
        ILogger log)
    {
        _fileSystem = fileSystem;
        _storagePath = storagePath;
        _configProvider = configProvider;
        Log = log;
        _hash = FNV1aFactory.Instance.Create(FNVConfig.GetPredefinedConfig(32));
    }

    private ILogger Log { get; }

    public T? Load<T>() where T : class
    {
        var path = PathFromAttribute<T>();
        if (!_fileSystem.File.Exists(path))
        {
            return null;
        }

        var json = _fileSystem.File.ReadAllText(path);

        try
        {
            return JObject.Parse(json).ToObject<T>();
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
        _fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(path));
        _fileSystem.File.WriteAllText(path, JsonConvert.SerializeObject(obj, new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            }
        }));
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

        return Path.Combine(_storagePath.Path, BuildServiceGuid(), objectName + ".json");
    }
}
