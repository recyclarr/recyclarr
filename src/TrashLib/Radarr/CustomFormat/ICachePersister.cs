using TrashLib.Radarr.CustomFormat.Models;
using TrashLib.Radarr.CustomFormat.Models.Cache;

namespace TrashLib.Radarr.CustomFormat;

public interface ICachePersister
{
    CustomFormatCache? CfCache { get; }
    void Load();
    void Save();
    void Update(IEnumerable<ProcessedCustomFormatData> customFormats);
}
