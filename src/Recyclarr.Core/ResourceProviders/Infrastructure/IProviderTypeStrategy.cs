using System.IO.Abstractions;
using Recyclarr.Settings.Models;

namespace Recyclarr.ResourceProviders.Infrastructure;

public interface IProviderTypeStrategy
{
    string Type { get; }

    IReadOnlyCollection<ResourceProvider> GetInitialProviders(ResourceProviderSettings settings);

    void MapResourcePaths(
        ResourceProvider config,
        IDirectoryInfo rootPath,
        IResourcePathRegistry registry
    );
}
