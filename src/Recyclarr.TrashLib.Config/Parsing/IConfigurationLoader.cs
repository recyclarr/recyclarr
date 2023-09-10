using System.IO.Abstractions;

namespace Recyclarr.TrashLib.Config.Parsing;

public interface IConfigurationLoader
{
    IReadOnlyCollection<IServiceConfiguration> Load(IFileInfo file);
    IReadOnlyCollection<IServiceConfiguration> Load(string yaml);
}
