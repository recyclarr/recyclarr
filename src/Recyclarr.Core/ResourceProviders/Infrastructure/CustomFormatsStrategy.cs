using System.IO.Abstractions;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Settings.Models;

namespace Recyclarr.ResourceProviders.Infrastructure;

public class CustomFormatsStrategy : IProviderTypeStrategy
{
    public string Type => "custom-formats";

    public IReadOnlyCollection<ResourceProvider> GetInitialProviders(
        ResourceProviderSettings settings
    )
    {
        return [];
    }

    public void MapResourcePaths(
        ResourceProvider config,
        IDirectoryInfo rootPath,
        IResourcePathRegistry registry
    )
    {
        var files = rootPath.EnumerateFiles("*.json", SearchOption.AllDirectories);

        if (config is LocalResourceProvider localProvider)
        {
            if (localProvider.Service == "radarr")
            {
                registry.Register<RadarrCustomFormatResource>(files);
            }
            else if (localProvider.Service == "sonarr")
            {
                registry.Register<SonarrCustomFormatResource>(files);
            }
        }
    }
}
