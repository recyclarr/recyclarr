using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Config.Parsing;

public interface IConfigRegistry
{
    IReadOnlyCollection<IServiceConfiguration> GetConfigsOfType(SupportedServices? serviceType);
    bool DoesConfigExist(string name);
}
