using System.IO.Abstractions;
using Recyclarr.Platform;

namespace Recyclarr.ResourceProviders.Storage;

public class ResourceCacheCleanupService(IAppPaths appPaths, ILogger log)
    : IResourceCacheCleanupService
{
    public void CleanupOrphans(IEnumerable<IDirectoryInfo> activePaths)
    {
        if (!appPaths.ResourceDirectory.Exists)
        {
            return;
        }

        try
        {
            var activeSet = activePaths.Select(p => p.FullName).ToHashSet();
            var allCachePaths = appPaths
                .ResourceDirectory.EnumerateDirectories()
                .SelectMany(typeDir =>
                    typeDir
                        .EnumerateDirectories()
                        .SelectMany(locationDir => locationDir.EnumerateDirectories())
                );

            foreach (var orphan in allCachePaths.Where(p => !activeSet.Contains(p.FullName)))
            {
                log.Debug("Deleting orphaned cache: {Path}", orphan.FullName);
                orphan.Delete(true);
            }
        }
#pragma warning disable CA1031
        catch (Exception e)
#pragma warning restore CA1031
        {
            log.Warning(e, "Failed to cleanup orphaned caches");
        }
    }
}
