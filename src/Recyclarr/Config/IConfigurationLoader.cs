using TrashLib.Config.Services;

namespace Recyclarr.Config;

public interface IConfigurationLoader<out T>
    where T : IServiceConfiguration
{
    IEnumerable<T> LoadMany(IEnumerable<string> configFiles, string configSection);
}
