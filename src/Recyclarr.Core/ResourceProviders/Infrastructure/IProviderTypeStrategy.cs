using System.IO.Abstractions;
using Recyclarr.Settings.Models;

namespace Recyclarr.ResourceProviders.Infrastructure;

public interface IProviderTypeStrategy
{
    string Type { get; }

    IReadOnlyCollection<ResourceProvider> GetInitialProviders(
        IReadOnlyCollection<ResourceProvider> providers
    );

    void MapResourcePaths(ResourceProvider config, IDirectoryInfo rootPath);
}
