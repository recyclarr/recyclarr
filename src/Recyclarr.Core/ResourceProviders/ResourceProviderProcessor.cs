using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.ResourceProviders;

public class ResourceProviderProcessor(
    IEnumerable<IResourceProvider> providers,
    ISettings<ResourceProviderSettings> settings,
    ILogger logger
)
{
    public async Task ProcessResourceProviders(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resourceProviderSettings = settings.Value;

        // Resource providers now use lazy initialization - no explicit initialization needed
        var configuredProviders = GetConfiguredProviders(resourceProviderSettings);

        logger.Information(
            "Configured {ProviderCount} resource providers: {ProviderNames}",
            configuredProviders.Count(),
            string.Join(", ", configuredProviders.Select(p => p.Name))
        );

        await Task.CompletedTask; // No async work needed anymore
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
