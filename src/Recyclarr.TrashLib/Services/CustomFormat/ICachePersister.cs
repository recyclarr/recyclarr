using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Services.CustomFormat.Models;
using Recyclarr.TrashLib.Services.CustomFormat.Models.Cache;

namespace Recyclarr.TrashLib.Services.CustomFormat;

public interface ICachePersister
{
    CustomFormatCache? CfCache { get; }
    void Load(IServiceConfiguration config);
    void Save(IServiceConfiguration config);
    void Update(IEnumerable<ProcessedCustomFormatData> customFormats);
}
