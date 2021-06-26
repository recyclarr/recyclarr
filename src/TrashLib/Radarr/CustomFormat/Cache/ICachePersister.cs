using System.Collections.Generic;
using TrashLib.Radarr.CustomFormat.Models;
using TrashLib.Radarr.CustomFormat.Models.Cache;

namespace TrashLib.Radarr.CustomFormat.Cache
{
    public interface ICachePersister
    {
        List<TrashIdMapping> CfCache { get; }
        void Load();
        void Save();
        void Update(IEnumerable<ProcessedCustomFormatData> customFormats);
    }
}
