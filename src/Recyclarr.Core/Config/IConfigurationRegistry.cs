using Recyclarr.Config.Models;

namespace Recyclarr.Config;

public interface IConfigurationRegistry
{
    IReadOnlyCollection<IServiceConfiguration> FindAndLoadConfigs(
        ConfigFilterCriteria? filterCriteria = null
    );
}
