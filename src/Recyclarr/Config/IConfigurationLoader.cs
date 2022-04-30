using TrashLib.Config.Services;

namespace Recyclarr.Config;

public interface IConfigurationLoader<out T>
    where T : IServiceConfiguration
{
    IEnumerable<T> Load(string propertyName, string configSection);
    IEnumerable<T> LoadFromStream(TextReader stream, string configSection);
    IEnumerable<T> LoadMany(IEnumerable<string> configFiles, string configSection);
}
