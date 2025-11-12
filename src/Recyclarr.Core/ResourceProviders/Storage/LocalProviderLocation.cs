using System.IO.Abstractions;
using Recyclarr.Settings.Models;

namespace Recyclarr.ResourceProviders.Storage;

public class LocalProviderLocation(
    LocalResourceProvider config,
    IFileSystem fileSystem,
    ILogger log
) : IProviderLocation
{
    public Task<IReadOnlyCollection<IDirectoryInfo>> InitializeAsync(
        IProgress<ProviderProgress>? progress,
        CancellationToken ct
    )
    {
        var path = fileSystem.DirectoryInfo.New(config.Path);

        if (!path.Exists)
        {
            log.Warning(
                "Local provider {Name} path does not exist: {Path}",
                config.Name,
                config.Path
            );
            return Task.FromResult<IReadOnlyCollection<IDirectoryInfo>>([]);
        }

        return Task.FromResult<IReadOnlyCollection<IDirectoryInfo>>([path]);
    }
}
