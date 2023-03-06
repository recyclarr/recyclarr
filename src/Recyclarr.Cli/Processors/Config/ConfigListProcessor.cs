using Autofac.Features.Indexed;
using Recyclarr.TrashLib.Config.Listers;
using Recyclarr.TrashLib.Repo;

namespace Recyclarr.Cli.Processors.Config;

public class ConfigListProcessor
{
    private readonly ILogger _log;
    private readonly IIndex<ConfigListCategory, IConfigLister> _configListers;
    private readonly IConfigTemplatesRepo _repo;

    public ConfigListProcessor(
        ILogger log,
        IIndex<ConfigListCategory, IConfigLister> configListers,
        IConfigTemplatesRepo repo)
    {
        _log = log;
        _configListers = configListers;
        _repo = repo;
    }

    public async Task Process(ConfigListCategory listCategory)
    {
        if (listCategory == ConfigListCategory.Templates)
        {
            await _repo.Update();
        }

        _log.Debug("Listing configuration for category {Category}", listCategory);
        if (!_configListers.TryGetValue(listCategory, out var lister))
        {
            throw new ArgumentOutOfRangeException(nameof(listCategory), listCategory, "Unknown list category");
        }

        await lister.List();
    }
}
