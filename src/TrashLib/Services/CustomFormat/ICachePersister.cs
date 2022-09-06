using TrashLib.Services.CustomFormat.Models;
using TrashLib.Services.CustomFormat.Models.Cache;

namespace TrashLib.Services.CustomFormat;

public interface ICachePersister
{
    CustomFormatCache? CfCache { get; }
    void Load();
    void Save();
    void Update(IEnumerable<ProcessedCustomFormatData> customFormats);
}
