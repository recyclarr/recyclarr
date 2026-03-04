using System.IO.Abstractions;
using Recyclarr.Common.Extensions;
using Recyclarr.Platform;

namespace Recyclarr.Cli.Migration.Steps;

[UsedImplicitly]
[MigrationOrder(2)]
internal class MoveCacheToStateMigrationStep(IAppPaths paths) : IMigrationStep
{
    public string Description => "Move cache directory to state";
    public IReadOnlyCollection<string> Remediation =>
        [
            $"Ensure Recyclarr has permission to read/write {CacheDir} and {StateDir}",
            $"Manually move {CacheDir}/sonarr and {CacheDir}/radarr to {StateDir}",
            $"Manually move {CacheDir}/resources to {paths.ResourceDirectory}",
            $"Delete {CacheDir} after moving contents",
        ];

    private IDirectoryInfo CacheDir => paths.ConfigDirectory.SubDirectory("cache");
    private IDirectoryInfo StateDir => paths.ConfigDirectory.SubDirectory("state");
    private IDirectoryInfo ResourcesDir => paths.ResourceDirectory;

    public bool CheckIfNeeded()
    {
        return CacheDir.Exists;
    }

    public void Execute(ILogger log)
    {
        var totalFiles = 0;
        var services = new List<string>();

        totalFiles += MoveServiceCaches(log, "sonarr", services);
        totalFiles += MoveServiceCaches(log, "radarr", services);
        MoveResources(log);
        DeleteEmptyCacheDir(log);

        if (totalFiles > 0)
        {
            log.Information(
                "Cache migration complete: moved {FileCount} files across {Services}",
                totalFiles,
                string.Join(", ", services)
            );
        }
        else
        {
            log.Warning("Cache directory exists but contains no service data to migrate");
        }
    }

    private int MoveServiceCaches(ILogger log, string serviceName, List<string> services)
    {
        var sourceDir = CacheDir.SubDirectory(serviceName);
        if (!sourceDir.Exists)
        {
            return 0;
        }

        var targetDir = StateDir.SubDirectory(serviceName);
        targetDir.Create();

        var fileCount = 0;
        var hashDirCount = 0;

        // Copy and rename files from each hash subdirectory, then delete originals
        foreach (var hashDir in sourceDir.EnumerateDirectories())
        {
            var targetHashDir = targetDir.SubDirectory(hashDir.Name);
            targetHashDir.Create();
            hashDirCount++;

            foreach (var file in hashDir.EnumerateFiles("*.json"))
            {
                var newName = file.Name.Replace(
                    "-cache.json",
                    "-mappings.json",
                    StringComparison.Ordinal
                );
                var targetPath = targetHashDir.File(newName);

                // Copy then delete to handle cross-filesystem moves (e.g. Docker volume mounts)
                file.CopyTo(targetPath.FullName);
                file.Delete();
                fileCount++;

                log.Debug("Migrated {Source} to {Target}", file.FullName, targetPath.FullName);
            }

            if (!hashDir.EnumerateFileSystemInfos().Any())
            {
                hashDir.Delete();
            }
        }

        if (!sourceDir.EnumerateFileSystemInfos().Any())
        {
            sourceDir.Delete();
        }

        if (fileCount > 0)
        {
            services.Add(serviceName);
            log.Information(
                "Migrated {Service}: {FileCount} files across {HashDirCount} instances",
                serviceName,
                fileCount,
                hashDirCount
            );
        }

        return fileCount;
    }

    private void MoveResources(ILogger log)
    {
        var sourceResourcesDir = CacheDir.SubDirectory("resources");
        if (!sourceResourcesDir.Exists)
        {
            return;
        }

        if (ResourcesDir.Exists)
        {
            sourceResourcesDir.RecursivelyDeleteReadOnly();
            log.Information("Deleted old cache/resources (target already exists)");
        }
        else
        {
            // Copy then delete to handle cross-filesystem moves (e.g. Docker volume mounts)
            sourceResourcesDir.CopyTo(ResourcesDir, recursive: true);
            sourceResourcesDir.RecursivelyDeleteReadOnly();
            log.Information("Moved resources from cache to {Target}", ResourcesDir.FullName);
        }
    }

    private void DeleteEmptyCacheDir(ILogger log)
    {
        if (CacheDir.Exists && !CacheDir.EnumerateFileSystemInfos().Any())
        {
            CacheDir.Delete();
            log.Debug("Deleted empty cache directory");
        }
    }
}
