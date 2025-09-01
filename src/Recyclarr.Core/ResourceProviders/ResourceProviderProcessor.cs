using System.Diagnostics.CodeAnalysis;
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

        // Initialize all configured resource providers
        var configuredProviders = GetConfiguredProviders(resourceProviderSettings);

        // Initialize providers individually to handle exceptions gracefully
        var initializationTasks = configuredProviders.Select(InitializeProviderSafely);
        await Task.WhenAll(initializationTasks);

        return;

        [SuppressMessage(
            "Design",
            "CA1031:Do not catch general exception types",
            Justification = "Intentionally catching all exceptions to prevent one provider failure from stopping initialization of other providers"
        )]
        async Task InitializeProviderSafely(IResourceProvider provider)
        {
            try
            {
                await provider.Initialize(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to initialize provider {ProviderName}", provider.Name);
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
