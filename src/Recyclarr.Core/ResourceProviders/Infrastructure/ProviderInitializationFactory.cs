using System.IO.Abstractions;
using Recyclarr.ResourceProviders.Storage;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.ResourceProviders.Infrastructure;

public class ProviderInitializationFactory(
    ISettings<IReadOnlyCollection<ResourceProvider>> settings,
    IEnumerable<IProviderTypeStrategy> strategies,
    IResourceCacheCleanupService cleanup,
    GitProviderLocation.Factory createGitLocation,
    LocalProviderLocation.Factory createLocalLocation,
    ILogger log
)
{
    public async Task InitializeProvidersAsync(
        IProgress<ProviderProgress>? progress,
        CancellationToken ct
    )
    {
        var strategyByType = strategies.ToDictionary(s => s.Type);

        var providers = strategies
            .SelectMany(s => s.GetInitialProviders(settings.Value))
            .Concat(settings.Value)
            .ToList();

        progress?.Report(
            new ProviderProgress("", ProviderStatus.Starting, TotalProviders: providers.Count)
        );

        var activePaths = new List<IDirectoryInfo>();

        foreach (var config in providers)
        {
            log.Information(
                "Initializing provider: {Name} (type: {Type})",
                config.Name,
                config.Type
            );

            var strategy = strategyByType[config.Type];

            try
            {
                var location = CreateLocation(config, strategy);
                var roots = await location.InitializeAsync(progress, ct);
                activePaths.AddRange(roots);

                foreach (var root in roots)
                {
                    strategy.MapResourcePaths(config, root);
                }
            }
            catch (Exception e)
            {
                progress?.Report(
                    new ProviderProgress(config.Name, ProviderStatus.Failed, e.Message)
                );
                throw;
            }
        }

        cleanup.CleanupOrphans(activePaths);
    }

    private IProviderLocation CreateLocation(
        ResourceProvider config,
        IProviderTypeStrategy strategy
    )
    {
        return config switch
        {
            GitResourceProvider git => createGitLocation(git, strategy),
            LocalResourceProvider local => createLocalLocation(local),
            _ => throw new InvalidOperationException(
                $"Unknown provider type: {config.GetType().Name}"
            ),
        };
    }
}
