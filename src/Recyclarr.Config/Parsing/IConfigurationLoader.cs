using System.IO.Abstractions;
using Recyclarr.Config.Models;

namespace Recyclarr.Config.Parsing;

public interface IConfigurationLoader
{
    IReadOnlyCollection<IServiceConfiguration> Load(IFileInfo file);
    IReadOnlyCollection<IServiceConfiguration> Load(string yaml);
}
