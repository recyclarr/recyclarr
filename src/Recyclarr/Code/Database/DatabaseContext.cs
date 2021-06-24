using Microsoft.EntityFrameworkCore;
using TrashLib.Radarr.Config;

namespace Recyclarr.Code.Database
{
    public class DatabaseContext : DbContext
    {
        public DbSet<RadarrConfig> RadarrConfigs { get; set; }
    }
}
