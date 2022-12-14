using Recyclarr.TrashLib.Services.CustomFormat.Models;
using Recyclarr.TrashLib.Services.CustomFormat.Models.Cache;

namespace Recyclarr.TrashLib.Services.CustomFormat;

public interface ICachePersister
{
    CustomFormatCache? CfCache { get; }
    void Load();
    void Save();
    void Update(IEnumerable<ProcessedCustomFormatData> customFormats);
}
