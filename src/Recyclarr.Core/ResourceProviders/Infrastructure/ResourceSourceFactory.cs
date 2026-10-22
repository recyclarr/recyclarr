using Recyclarr.ResourceProviders.Storage;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.ResourceProviders.Infrastructure;

/// <summary>
/// Creates one source per configured provider, implicit official providers first, in precedence
/// order (later sources override earlier ones).
/// </summary>
public class ResourceSourceFactory(
    ISettings<IReadOnlyCollection<ResourceProvider>> settings,
    IEnumerable<IProviderTypeStrategy> strategies,
    GitProviderLocation.Factory createGitLocation,
    LocalProviderLocation.Factory createLocalLocation
)
{
    public IReadOnlyList<IResourceSource> CreateAll()
    {
        var strategyList = strategies.ToList();
        var strategyByType = strategyList.ToDictionary(s => s.Type);

        return strategyList
            .SelectMany(s => s.GetInitialProviders(settings.Value))
            .Concat(settings.Value)
            .Select(config => CreateSource(config, strategyByType[config.Type]))
            .ToList();
    }

    private IResourceSource CreateSource(ResourceProvider config, IProviderTypeStrategy strategy)
    {
        return config switch
        {
            GitResourceProvider git => createGitLocation(git, strategy),
            LocalResourceProvider local => createLocalLocation(local, strategy),
            _ => throw new InvalidOperationException(
                $"Unknown provider type: {config.GetType().Name}"
            ),
        };
    }
}
