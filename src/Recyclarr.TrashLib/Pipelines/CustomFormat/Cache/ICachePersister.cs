using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Pipelines.CustomFormat.Models;

namespace Recyclarr.TrashLib.Pipelines.CustomFormat.Cache;

public interface ICachePersister
{
    CustomFormatCache Load(IServiceConfiguration config);
    void Save(IServiceConfiguration config, CustomFormatCache cache);
}
