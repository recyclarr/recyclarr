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
            var locationDirs = appPaths
                .ResourceDirectory.EnumerateDirectories()
                .SelectMany(typeDir => typeDir.EnumerateDirectories())
                .ToList();

            var orphanCaches = locationDirs
                .SelectMany(locationDir => locationDir.EnumerateDirectories())
                .Where(p => !activeSet.Contains(p.FullName));

            foreach (var orphan in orphanCaches)
            {
                log.Debug("Deleting orphaned cache: {Path}", orphan.FullName);
                orphan.Delete(true);
            }

            // A refresh state file sits beside its cache directory as `<name>.state.json`.
            var orphanStateFiles = locationDirs
                .SelectMany(locationDir => locationDir.EnumerateFiles("*.state.json"))
                .Where(f => !activeSet.Contains(f.FullName[..^".state.json".Length]));

            foreach (var orphan in orphanStateFiles)
            {
                log.Debug("Deleting orphaned refresh state: {Path}", orphan.FullName);
                orphan.Delete();
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
