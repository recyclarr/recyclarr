using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Cache;

public interface ICachePersister
{
    CustomFormatCache Load(IServiceConfiguration config);
    void Save(IServiceConfiguration config, CustomFormatCache cache);
}
