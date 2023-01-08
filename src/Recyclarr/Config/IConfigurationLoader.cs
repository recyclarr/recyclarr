using System.IO.Abstractions;
using TrashLib.Config.Services;

namespace Recyclarr.Config;

public interface IConfigurationLoader<out T>
    where T : IServiceConfiguration
{
    IEnumerable<T> LoadMany(IEnumerable<IFileInfo> configFiles, string configSection);
}
