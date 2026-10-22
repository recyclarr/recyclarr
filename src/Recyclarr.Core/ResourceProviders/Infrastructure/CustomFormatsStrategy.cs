using System.IO.Abstractions;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Settings.Models;

namespace Recyclarr.ResourceProviders.Infrastructure;

public class CustomFormatsStrategy : IProviderTypeStrategy
{
    public string Type => "custom-formats";

    public IReadOnlyList<string> DefaultReferences => ["master", "main"];

    public IReadOnlyCollection<ResourceProvider> GetInitialProviders(
        IReadOnlyCollection<ResourceProvider> providers
    )
    {
        return [];
    }

    public void MapResourcePaths(ResourceProvider config, IDirectoryInfo rootPath, GuideIndex index)
    {
        var files = rootPath.EnumerateFiles("*.json", SearchOption.AllDirectories);

        switch (config.Service)
        {
            case "radarr":
                index.Files.Register<RadarrCustomFormatResource>(files);
                break;
            case "sonarr":
                index.Files.Register<SonarrCustomFormatResource>(files);
                break;
            default:
                throw new InvalidOperationException(
                    $"Invalid service '{config.Service}' for custom-formats provider '{config.Name}'. "
                        + "Expected 'radarr' or 'sonarr'."
                );
        }
    }
}
