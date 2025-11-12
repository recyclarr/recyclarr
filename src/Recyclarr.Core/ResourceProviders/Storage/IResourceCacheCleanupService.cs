using System.IO.Abstractions;

namespace Recyclarr.ResourceProviders.Storage;

public interface IResourceCacheCleanupService
{
    void CleanupOrphans(IEnumerable<IDirectoryInfo> activePaths);
}
