using System.IO.Abstractions;

namespace Recyclarr.Config.Parsing;

public interface IConfigurationFinder
{
    IReadOnlyCollection<IFileInfo> GetConfigFiles();
}
