using Microsoft.EntityFrameworkCore;
using TrashLib.Radarr.Config;
using TrashLib.Radarr.CustomFormat.Models.Cache;

namespace Recyclarr.Code.Radarr
{
    public class RadarrDatabaseContext : DbContext
    {
        public DbSet<RadarrConfig> RadarrConfigs { get; set; }
        public DbSet<TrashIdMapping> CustomFormatCache { get; set; }
    }
}
