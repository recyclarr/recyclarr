using System.IO.Abstractions;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Config.Parsing;

public interface IConfigurationLoader
{
    IReadOnlyCollection<IServiceConfiguration> Load(IFileInfo file);
    IReadOnlyCollection<IServiceConfiguration> Load(string yaml);
}
