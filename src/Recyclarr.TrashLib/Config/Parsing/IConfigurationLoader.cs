using System.IO.Abstractions;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Config.Parsing;

public interface IConfigurationLoader
{
    ICollection<IServiceConfiguration> LoadMany(
        IEnumerable<IFileInfo> configFiles,
        SupportedServices? desiredServiceType = null);

    IReadOnlyCollection<IServiceConfiguration> Load(IFileInfo file, SupportedServices? desiredServiceType = null);
    IReadOnlyCollection<IServiceConfiguration> Load(string yaml, SupportedServices? desiredServiceType = null);
}
