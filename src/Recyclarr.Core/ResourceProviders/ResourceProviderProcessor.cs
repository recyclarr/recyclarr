using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.ResourceProviders;

public class ResourceProviderProcessor(
    IEnumerable<IResourceProvider> providers,
    ISettings<ResourceProviderSettings> settings
)
{
    public async Task ProcessResourceProviders(CancellationToken cancellationToken = default)
    {
        var resourceProviderSettings = settings.Value;

        // Initialize all configured resource providers
        var configuredProviders = GetConfiguredProviders(resourceProviderSettings);

        await Task.WhenAll(
            configuredProviders.Select(provider => provider.Initialize(cancellationToken))
        );
    }

    private IEnumerable<IResourceProvider> GetConfiguredProviders(
        ResourceProviderSettings resourceProviderSettings
    )
    {
        // For now, return all providers since we don't have the settings model updated yet
        // This will be updated when we implement the settings transformation
        // The resourceProviderSettings parameter will be used to filter providers based on configuration
        _ = resourceProviderSettings; // Acknowledge parameter to suppress warning
        return providers;
    }
}
