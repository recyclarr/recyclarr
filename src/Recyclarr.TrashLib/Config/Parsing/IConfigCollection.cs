using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Config.Parsing;

public interface IConfigCollection
{
    IReadOnlyCollection<T> Get<T>(SupportedServices serviceType) where T : ServiceConfiguration;
    bool DoesConfigExist(string name);
}
