using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Pipelines.CustomFormat.Cache;

public interface ICustomFormatCachePersister
{
    CustomFormatCache Load(IServiceConfiguration config);
    void Save(IServiceConfiguration config, CustomFormatCache cache);
}
