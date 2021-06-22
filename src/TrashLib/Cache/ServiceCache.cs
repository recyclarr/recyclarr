using System;
using System.IO;
using System.IO.Abstractions;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Serilog;

namespace TrashLib.Cache
{
    internal class ServiceCache : IServiceCache
    {
        private static readonly Regex AllowedObjectNameCharacters = new(@"^[\w-]+$", RegexOptions.Compiled);
        private readonly IFileSystem _fileSystem;
        private readonly ICacheStoragePath _storagePath;

        public ServiceCache(
            IFileSystem fileSystem,
            ICacheStoragePath storagePath,
            ILogger log)
        {
            _fileSystem = fileSystem;
            _storagePath = storagePath;
            Log = log;
        }

        private ILogger Log { get; }

        public T? Load<T>(ICacheGuidBuilder guidBuilder) where T : class
        {
            var path = PathFromAttribute<T>(guidBuilder);
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

        public void Save<T>(T obj, ICacheGuidBuilder guidBuilder) where T : class
        {
            var path = PathFromAttribute<T>(guidBuilder);
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

        private string PathFromAttribute<T>(ICacheGuidBuilder guidBuilder)
        {
            var objectName = GetCacheObjectNameAttribute<T>();
            if (!AllowedObjectNameCharacters.IsMatch(objectName))
            {
                throw new ArgumentException($"Object name '{objectName}' has unacceptable characters");
            }

            return Path.Combine(_storagePath.Path, guidBuilder.MakeGuid(), objectName + ".json");
        }
    }
}
