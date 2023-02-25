using Autofac.Features.Indexed;
using Recyclarr.TrashLib.Config.Listers;

namespace Recyclarr.TrashLib.Processors;

public class ConfigListProcessor
{
    private readonly ILogger _log;
    private readonly IIndex<ConfigListCategory, IConfigLister> _configListers;

    public ConfigListProcessor(ILogger log, IIndex<ConfigListCategory, IConfigLister> configListers)
    {
        _log = log;
        _configListers = configListers;
    }

    public void Process(ConfigListCategory listCategory)
    {
        _log.Debug("Listing configuration for category {Category}", listCategory);
        if (!_configListers.TryGetValue(listCategory, out var lister))
        {
            throw new ArgumentOutOfRangeException(nameof(listCategory), listCategory, "Unknown list category");
        }

        lister.List();
    }
}
