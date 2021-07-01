using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TrashLib.Config;
using TrashLib.Radarr.CustomFormat.Cache;
using TrashLib.Radarr.CustomFormat.Models;
using TrashLib.Radarr.CustomFormat.Models.Cache;

namespace Recyclarr.Code.Radarr
{
    internal class CustomFormatCache : ICustomFormatCache
    {
        private readonly IServiceConfiguration _config;
        private readonly RadarrDatabaseContext _context;

        public CustomFormatCache(IDbContextFactory<RadarrDatabaseContext> contextFactory, IServiceConfiguration config)
        {
            _config = config;
            _context = contextFactory.CreateDbContext();
        }

        public IEnumerable<TrashIdMapping> Mappings =>
            _context.CustomFormatCache.Where(c => c.ServiceBaseUrl == _config.BaseUrl);

        public void Add(int formatId, ProcessedCustomFormatData format)
        {
            _context.CustomFormatCache.Add(new TrashIdMapping
            {
                ServiceBaseUrl = _config.BaseUrl,
                CustomFormatId = formatId,
                TrashId = format.TrashId
            });
        }

        public void Remove(TrashIdMapping cfId)
        {
            _context.CustomFormatCache.Remove(cfId);
        }
    }
}
