using System.IO.Abstractions;

namespace Recyclarr.ResourceProviders.Storage;

public interface IProviderLocation
{
    Task<IReadOnlyCollection<IDirectoryInfo>> InitializeAsync(
        IProgress<ProviderProgress>? progress,
        CancellationToken ct
    );
}
