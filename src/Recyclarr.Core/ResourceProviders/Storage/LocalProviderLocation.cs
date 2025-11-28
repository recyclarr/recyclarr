using System.IO.Abstractions;
using Recyclarr.Settings.Models;

namespace Recyclarr.ResourceProviders.Storage;

public class LocalProviderLocation(LocalResourceProvider config, IFileSystem fileSystem)
    : IProviderLocation
{
    public delegate LocalProviderLocation Factory(LocalResourceProvider config);

    public Task<IReadOnlyCollection<IDirectoryInfo>> InitializeAsync(
        IProgress<ProviderProgress>? progress,
        CancellationToken ct
    )
    {
        progress?.Report(new ProviderProgress(config.Type, config.Name, ProviderStatus.Processing));

        var path = fileSystem.DirectoryInfo.New(config.Path);

        if (!path.Exists)
        {
            progress?.Report(
                new ProviderProgress(
                    config.Type,
                    config.Name,
                    ProviderStatus.Failed,
                    $"Path does not exist: {config.Path}"
                )
            );
            return Task.FromResult<IReadOnlyCollection<IDirectoryInfo>>([]);
        }

        progress?.Report(new ProviderProgress(config.Type, config.Name, ProviderStatus.Completed));
        return Task.FromResult<IReadOnlyCollection<IDirectoryInfo>>([path]);
    }
}
