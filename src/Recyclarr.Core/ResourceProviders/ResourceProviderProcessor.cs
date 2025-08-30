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
        cancellationToken.ThrowIfCancellationRequested();
        
        var resourceProviderSettings = settings.Value;

        // Initialize all configured resource providers
        var configuredProviders = GetConfiguredProviders(resourceProviderSettings);

        // Initialize providers individually to handle exceptions gracefully
        var initializationTasks = configuredProviders.Select(InitializeProviderSafely);
        await Task.WhenAll(initializationTasks);

        async Task InitializeProviderSafely(IResourceProvider provider)
        {
            try
            {
                await provider.Initialize(cancellationToken);
            }
            catch (Exception ex)
            {
                // Log error and continue with other providers
                // In real implementation, this would use ILogger
                Console.WriteLine($"Failed to initialize provider {provider.Name}: {ex.Message}");
            }
        }
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
