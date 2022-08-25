using TrashLib.Services.Radarr.CustomFormat.Models;
using TrashLib.Services.Radarr.CustomFormat.Models.Cache;

namespace TrashLib.Services.Radarr.CustomFormat;

public interface ICachePersister
{
    CustomFormatCache? CfCache { get; }
    void Load();
    void Save();
    void Update(IEnumerable<ProcessedCustomFormatData> customFormats);
}
