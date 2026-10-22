using System.IO.Abstractions;
using Recyclarr.Platform;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.Settings.Models;

namespace Recyclarr.ResourceProviders.Storage;

// A directory the user maintains; Recyclarr never changes it, so there is nothing to update or
// revert. A missing directory contributes no data.
public class LocalProviderLocation(
    LocalResourceProvider config,
    IProviderTypeStrategy strategy,
    IFileSystem fileSystem,
    IAppPaths appPaths
) : IResourceSource
{
    public delegate LocalProviderLocation Factory(
        LocalResourceProvider config,
        IProviderTypeStrategy strategy
    );

    public ResourceProvider Config => config;
    public IProviderTypeStrategy Strategy => strategy;
    public SourceTracking? Tracking => null;

    public IReadOnlyCollection<IDirectoryInfo> Roots
    {
        get
        {
            var path = ResolvePath(config.Path);
            return path.Exists ? [path] : [];
        }
    }

    public Task<SourceUpdateOutcome> UpdateAsync(CancellationToken ct) =>
        Task.FromResult(SourceUpdateOutcome.Unchanged);

    public Task MarkGoodAsync(CancellationToken ct) => Task.CompletedTask;

    public Task<bool> RevertToGoodAsync(CancellationToken ct) => Task.FromResult(false);

    private IDirectoryInfo ResolvePath(string path)
    {
        if (fileSystem.Path.IsPathRooted(path))
        {
            return fileSystem.DirectoryInfo.New(path);
        }

        return appPaths.ConfigDirectory.SubDirectory(path);
    }
}
