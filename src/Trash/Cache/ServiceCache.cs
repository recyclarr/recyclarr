using System;
using System.IO;
using System.IO.Abstractions;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Trash.Cache
{
    public class ServiceCache : IServiceCache
    {
        private static readonly Regex AllowedObjectNameCharacters = new(@"^\w+$", RegexOptions.Compiled);
        private readonly IFileSystem _fileSystem;
        private readonly ICacheStoragePath _storagePath;

        public ServiceCache(IFileSystem fileSystem, ICacheStoragePath storagePath)
        {
            _fileSystem = fileSystem;
            _storagePath = storagePath;
        }

        public T Load<T>()
        {
            var json = _fileSystem.File.ReadAllText(PathFromAttribute<T>());
            return JObject.Parse(json).ToObject<T>();
        }

        public void Save<T>(T obj)
        {
            _fileSystem.File.WriteAllText(PathFromAttribute<T>(),
                JsonConvert.SerializeObject(obj, Formatting.Indented));
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

        private string PathFromAttribute<T>()
        {
            var objectName = GetCacheObjectNameAttribute<T>();
            if (!AllowedObjectNameCharacters.IsMatch(objectName))
            {
                throw new ArgumentException($"Object name '{objectName}' has unacceptable characters");
            }

            return Path.Join(_storagePath.Path, objectName + ".json");
        }
    }
}
