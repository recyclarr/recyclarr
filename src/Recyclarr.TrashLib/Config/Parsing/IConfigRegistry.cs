using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Processors;

namespace Recyclarr.TrashLib.Config.Parsing;

public interface IConfigRegistry
{
    int Count { get; }
    bool DoesConfigExist(string name);
    IEnumerable<IServiceConfiguration> GetConfigsBasedOnSettings(ISyncSettings settings);
    IEnumerable<IServiceConfiguration> GetAllConfigs();
    IEnumerable<IServiceConfiguration> GetConfigsOfType(SupportedServices? serviceType);
}
