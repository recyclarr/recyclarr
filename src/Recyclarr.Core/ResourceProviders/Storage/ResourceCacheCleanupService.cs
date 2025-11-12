using System.IO.Abstractions;
using Recyclarr.Platform;

namespace Recyclarr.ResourceProviders.Storage;

public class ResourceCacheCleanupService(IAppPaths appPaths, ILogger log)
    : IResourceCacheCleanupService
{
    public void CleanupOrphans(IEnumerable<IDirectoryInfo> activePaths)
    {
        if (!appPaths.ReposDirectory.Exists)
        {
            return;
        }

        try
        {
            var activeSet = activePaths.Select(p => p.FullName).ToHashSet();
            var allCachePaths = appPaths.ReposDirectory.EnumerateDirectories(
                "*/*/*",
                SearchOption.TopDirectoryOnly
            );

            foreach (var orphan in allCachePaths.Where(p => !activeSet.Contains(p.FullName)))
            {
                log.Debug("Deleting orphaned cache: {Path}", orphan.FullName);
                orphan.Delete(recursive: true);
            }
        }
        catch (Exception e)
        {
            log.Warning(e, "Failed to cleanup orphaned caches");
        }
    }
}
