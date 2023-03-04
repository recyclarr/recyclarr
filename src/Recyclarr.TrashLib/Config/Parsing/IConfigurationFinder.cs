using System.IO.Abstractions;

namespace Recyclarr.TrashLib.Config.Parsing;

public interface IConfigurationFinder
{
    IReadOnlyCollection<IFileInfo> GetConfigFiles(IReadOnlyCollection<IFileInfo>? configs = null);
}
