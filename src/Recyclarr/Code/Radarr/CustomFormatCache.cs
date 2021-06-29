using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TrashLib.Config;
using TrashLib.Radarr.CustomFormat.Cache;
using TrashLib.Radarr.CustomFormat.Models.Cache;

namespace Recyclarr.Code.Radarr
{
    internal class CustomFormatCache : ICustomFormatCache
    {
        private readonly RadarrDatabaseContext _context;

        public CustomFormatCache(IDbContextFactory<RadarrDatabaseContext> contextFactory)
        {
            _context = contextFactory.CreateDbContext();
        }

        public IEnumerable<TrashIdMapping> Load(IServiceConfiguration config)
        {
            return _context.CustomFormatCache
                .Where(c => c.ServiceBaseUrl == config.BaseUrl);
        }
    }
}
