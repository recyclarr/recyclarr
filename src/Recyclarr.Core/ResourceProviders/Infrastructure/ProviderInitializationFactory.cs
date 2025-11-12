using System.IO.Abstractions;
using Autofac.Features.Indexed;
using Recyclarr.ResourceProviders.Storage;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.ResourceProviders.Infrastructure;

public delegate GitProviderLocation GitLocationFactory(GitResourceProvider config);
public delegate LocalProviderLocation LocalLocationFactory(LocalResourceProvider config);

public class ProviderInitializationFactory(
    ISettings<ResourceProviderSettings> settings,
    IIndex<string, IProviderTypeStrategy> strategies,
    IResourcePathRegistry registry,
    IResourceCacheCleanupService cleanup,
    GitLocationFactory createGitLocation,
    LocalLocationFactory createLocalLocation,
    ILogger log
)
{
    public async Task InitializeProvidersAsync(
        IProgress<ProviderProgress>? progress,
        CancellationToken ct
    )
    {
        var allStrategies = new[] { "trash-guides", "config-templates", "custom-formats" }.Select(
            key => strategies[key]
        );

        var providers = allStrategies
            .SelectMany(s => s.GetInitialProviders(settings.Value))
            .Concat(settings.Value.Providers);

        var activePaths = new List<IDirectoryInfo>();

        foreach (var config in providers)
        {
            log.Information(
                "Initializing provider: {Name} (type: {Type})",
                config.Name,
                config.Type
            );

            try
            {
                var location = CreateLocation(config);
                var roots = await location.InitializeAsync(progress, ct);
                activePaths.AddRange(roots);

                var strategy = strategies[config.Type];
                foreach (var root in roots)
                {
                    strategy.MapResourcePaths(config, root, registry);
                }
            }
            catch (Exception e)
            {
                progress?.Report(
                    new ProviderProgress(config.Type, config.Name, ProviderStatus.Failed, e.Message)
                );
                log.Error(e, "Provider {Name} failed initialization", config.Name);
            }
        }

        cleanup.CleanupOrphans(activePaths);
    }

    private IProviderLocation CreateLocation(ResourceProvider config)
    {
        return config switch
        {
            GitResourceProvider git => createGitLocation(git),
            LocalResourceProvider local => createLocalLocation(local),
            _ => throw new InvalidOperationException(
                $"Unknown provider type: {config.GetType().Name}"
            ),
        };
    }
}
