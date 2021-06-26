using System.Collections.Generic;
using TrashLib.Config;
using TrashLib.Radarr.CustomFormat.Cache;
using TrashLib.Radarr.CustomFormat.Models.Cache;

namespace TrashLib.Cache
{
    public interface IServiceCache
    {
        IEnumerable<T> Load<T>(IServiceConfiguration config) where T : ServiceCacheObject;
        void Save<T>(IEnumerable<T> objList, IServiceConfiguration config) where T : ServiceCacheObject;
    }
}
