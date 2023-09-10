namespace Recyclarr.TrashLib.Config;

public interface IConfigurationRegistry
{
    IReadOnlyCollection<IServiceConfiguration> FindAndLoadConfigs(ConfigFilterCriteria? filterCriteria = null);
}
