using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Config.Parsing;

public interface IConfigRegistry
{
    IReadOnlyCollection<T> GetConfigsOfType<T>(SupportedServices serviceType) where T : ServiceConfiguration;
    bool DoesConfigExist(string name);
}
