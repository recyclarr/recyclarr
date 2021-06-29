using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using TrashLib.Config;
using TrashLib.Radarr.Config;

namespace Recyclarr.Code.Radarr
{
    public interface IConfigRepository<out T>
        where T : IServiceConfiguration
    {
        IEnumerable<T> Configs { get; }
        void Save();
        void Add(RadarrConfig item);
        void Remove(RadarrConfig item);
    }

    public class RadarrConfigRepository : IConfigRepository<RadarrConfig>
    {
        private readonly RadarrDatabaseContext _context;

        public RadarrConfigRepository(IDbContextFactory<RadarrDatabaseContext> contextFactory)
        {
            _context = contextFactory.CreateDbContext();
        }

        public IEnumerable<RadarrConfig> Configs => _context.RadarrConfigs;
        public void Save() => _context.SaveChanges();
        public void Add(RadarrConfig item) => _context.RadarrConfigs.Add(item);
        public void Remove(RadarrConfig item) => _context.RadarrConfigs.Remove(item);
    }
}
