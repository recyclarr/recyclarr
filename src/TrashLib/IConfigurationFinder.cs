using System.IO.Abstractions;

namespace TrashLib;

public interface IConfigurationFinder
{
    IFileInfo FindConfigPath();
}
