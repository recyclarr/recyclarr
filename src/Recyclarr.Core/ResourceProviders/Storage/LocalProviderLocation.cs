using System.IO.Abstractions;
using Recyclarr.Platform;
using Recyclarr.Settings.Models;

namespace Recyclarr.ResourceProviders.Storage;

public class LocalProviderLocation(
    LocalResourceProvider config,
    IFileSystem fileSystem,
    IAppPaths appPaths
) : IProviderLocation
{
    public delegate LocalProviderLocation Factory(LocalResourceProvider config);

    public Task<IReadOnlyCollection<IDirectoryInfo>> InitializeAsync(
        IProgress<ProviderProgress>? progress,
        CancellationToken ct
    )
    {
        progress?.Report(new ProviderProgress(config.Name, ProviderStatus.Processing));

        var path = ResolvePath(config.Path);

        if (!path.Exists)
        {
            progress?.Report(
                new ProviderProgress(
                    config.Name,
                    ProviderStatus.Failed,
                    $"Path does not exist: {config.Path}"
                )
            );
            return Task.FromResult<IReadOnlyCollection<IDirectoryInfo>>([]);
        }

        progress?.Report(new ProviderProgress(config.Name, ProviderStatus.Completed));
        return Task.FromResult<IReadOnlyCollection<IDirectoryInfo>>([path]);
    }

    private IDirectoryInfo ResolvePath(string path)
    {
        if (fileSystem.Path.IsPathRooted(path))
        {
            return fileSystem.DirectoryInfo.New(path);
        }

        return appPaths.ConfigDirectory.SubDirectory(path);
    }
}
