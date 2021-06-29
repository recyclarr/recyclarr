using System.Collections.Generic;
using TrashLib.Config;
using TrashLib.Radarr.CustomFormat.Models.Cache;

namespace TrashLib.Radarr.CustomFormat.Cache
{
    public interface ICustomFormatCache
    {
        IEnumerable<TrashIdMapping> Load(IServiceConfiguration config);
    }
}
