using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Serilog;
using TrashLib.Config;
using TrashLib.Radarr.CustomFormat.Cache;

namespace TrashLib.Cache
{
    internal class FilesystemServiceCache : IServiceCache
    {
        private readonly IFileSystem _fileSystem;
        private readonly ICacheGuidBuilder _guidBuilder;
        private readonly ICacheStoragePath _storagePath;

        public FilesystemServiceCache(
            IFileSystem fileSystem,
            ICacheStoragePath storagePath,
            ILogger log,
            ICacheGuidBuilder guidBuilder)
        {
            _fileSystem = fileSystem;
            _storagePath = storagePath;
            _guidBuilder = guidBuilder;
            Log = log;
        }

        private ILogger Log { get; }

        public IEnumerable<T> Load<T>(IServiceConfiguration config)
            where T : ServiceCacheObject
        {
            var path = PathFromAttribute<T>(config);
            if (!_fileSystem.File.Exists(path))
            {
                return Array.Empty<T>();
            }

            var json = _fileSystem.File.ReadAllText(path);

            try
            {
                return JObject.Parse(json).ToObject<IEnumerable<T>>();
            }
            catch (JsonException e)
            {
                Log.Error("Failed to read cache data, will proceed without cache. Reason: {Msg}", e.Message);
            }

            return Array.Empty<T>();
        }

        public void Save<T>(IEnumerable<T> objList, IServiceConfiguration config)
            where T : ServiceCacheObject
        {
            var path = PathFromAttribute<T>(config);
            _fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(path));
            _fileSystem.File.WriteAllText(path, JsonConvert.SerializeObject(objList, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            }));
        }

        private string PathFromAttribute<T>(IServiceConfiguration config)
        {
            var objectName = GetCacheObjectName<T>();
            return Path.Combine(_storagePath.Path, _guidBuilder.MakeGuid(config), objectName + ".json");
        }

        private static string GetCacheObjectName<T>()
        {
            return typeof(T).Name;
        }
    }
}
