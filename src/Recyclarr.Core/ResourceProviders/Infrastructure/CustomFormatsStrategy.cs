using System.IO.Abstractions;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Settings.Models;

namespace Recyclarr.ResourceProviders.Infrastructure;

public class CustomFormatsStrategy(ResourceRegistry<IFileInfo> registry) : IProviderTypeStrategy
{
    public string Type => "custom-formats";

    public IReadOnlyCollection<ResourceProvider> GetInitialProviders(
        IReadOnlyCollection<ResourceProvider> providers
    )
    {
        return [];
    }

    public void MapResourcePaths(ResourceProvider config, IDirectoryInfo rootPath)
    {
        var files = rootPath.EnumerateFiles("*.json", SearchOption.AllDirectories);

        switch (config.Service)
        {
            case "radarr":
                registry.Register<RadarrCustomFormatResource>(files);
                break;
            case "sonarr":
                registry.Register<SonarrCustomFormatResource>(files);
                break;
            default:
                throw new InvalidOperationException(
                    $"Invalid service '{config.Service}' for custom-formats provider '{config.Name}'. "
                        + "Expected 'radarr' or 'sonarr'."
                );
        }
    }
}
