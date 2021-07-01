using System.Collections.Generic;
using TrashLib.Radarr.CustomFormat.Models;
using TrashLib.Radarr.CustomFormat.Models.Cache;

namespace TrashLib.Radarr.CustomFormat.Cache
{
    public interface ICustomFormatCache
    {
        IEnumerable<TrashIdMapping> Mappings { get; }
        void Add(int formatId, ProcessedCustomFormatData format);
        void Remove(TrashIdMapping cfId);
        void Save();
    }
}
