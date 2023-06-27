using Autofac.Features.Indexed;
using Recyclarr.TrashLib.Config.Listers;

namespace Recyclarr.Cli.Processors.Config;

public class ConfigListProcessor
{
    private readonly ILogger _log;
    private readonly IIndex<ConfigCategory, IConfigLister> _configListers;

    public ConfigListProcessor(ILogger log, IIndex<ConfigCategory, IConfigLister> configListers)
    {
        _log = log;
        _configListers = configListers;
    }

    public async Task Process(ConfigCategory listCategory)
    {
        _log.Debug("Listing configuration for category {Category}", listCategory);
        if (!_configListers.TryGetValue(listCategory, out var lister))
        {
            throw new ArgumentOutOfRangeException(nameof(listCategory), listCategory, "Unknown list category");
        }

        await lister.List();
    }
}
