using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TrashLib.Cache;
using TrashLib.Config;
using TrashLib.Radarr.CustomFormat.Cache;

namespace Recyclarr.Code.Database
{
    internal class DatabaseServiceCache : IServiceCache
    {
        private readonly IDbContextFactory<DatabaseContext> _contextFactory;

        public DatabaseServiceCache(IDbContextFactory<DatabaseContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public IEnumerable<T> Load<T>(IServiceConfiguration config) where T : ServiceCacheObject
        {
            var context = _contextFactory.CreateDbContext();
            return context.Set<T>()
                .Where(r => r.ServiceBaseUrl == config.BaseUrl);
        }

        public void Save<T>(IEnumerable<T> objList, IServiceConfiguration config) where T : ServiceCacheObject
        {
            var context = _contextFactory.CreateDbContext();
            context.SaveChanges();
        }
    }
}
