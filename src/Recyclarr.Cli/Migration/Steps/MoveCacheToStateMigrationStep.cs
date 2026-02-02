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
            $"Manually move {CacheDir}/resources to {paths.AppDataDirectory.SubDirectory("resources")}",
            $"Delete {CacheDir} after moving contents",
        ];

    public bool Required => true;
    private IDirectoryInfo CacheDir => paths.AppDataDirectory.SubDirectory("cache");
    private IDirectoryInfo StateDir => paths.AppDataDirectory.SubDirectory("state");
    private IDirectoryInfo ResourcesDir => paths.AppDataDirectory.SubDirectory("resources");

    public bool CheckIfNeeded()
    {
        return CacheDir.Exists;
    }

    public void Execute(ILogger log)
    {
        MoveServiceCaches(log, "sonarr");
        MoveServiceCaches(log, "radarr");
        MoveResources(log);
        DeleteEmptyCacheDir(log);
    }

    private void MoveServiceCaches(ILogger log, string serviceName)
    {
        var sourceDir = CacheDir.SubDirectory(serviceName);
        if (!sourceDir.Exists)
        {
            return;
        }

        var targetDir = StateDir.SubDirectory(serviceName);
        targetDir.Create();

        // Move each hash subdirectory
        foreach (var hashDir in sourceDir.EnumerateDirectories())
        {
            var targetHashDir = targetDir.SubDirectory(hashDir.Name);
            targetHashDir.Create();

            // Move and rename JSON files: *-cache.json -> *-mappings.json
            foreach (var file in hashDir.EnumerateFiles("*.json"))
            {
                var newName = file.Name.Replace(
                    "-cache.json",
                    "-mappings.json",
                    StringComparison.Ordinal
                );
                var targetPath = targetHashDir.File(newName);
                file.MoveTo(targetPath.FullName);
                log.Debug("Moved {Source} to {Target}", file.FullName, targetPath.FullName);
            }

            // Delete empty source hash directory
            if (!hashDir.EnumerateFileSystemInfos().Any())
            {
                hashDir.Delete();
            }
        }

        // Delete empty source service directory
        if (!sourceDir.EnumerateFileSystemInfos().Any())
        {
            sourceDir.Delete();
        }

        log.Information("Migrated {Service} cache to state directory", serviceName);
    }

    private void MoveResources(ILogger log)
    {
        var sourceResourcesDir = CacheDir.SubDirectory("resources");
        if (!sourceResourcesDir.Exists)
        {
            return;
        }

        // Move resources to app data root (no longer under cache/state)
        if (ResourcesDir.Exists)
        {
            // Target exists - delete source since we have newer resources
            sourceResourcesDir.RecursivelyDeleteReadOnly();
            log.Information("Deleted old cache/resources (target already exists)");
        }
        else
        {
            sourceResourcesDir.MoveTo(ResourcesDir.FullName);
            log.Information("Moved resources from cache to app data root");
        }
    }

    private void DeleteEmptyCacheDir(ILogger log)
    {
        if (CacheDir.Exists && !CacheDir.EnumerateFileSystemInfos().Any())
        {
            CacheDir.Delete();
            log.Information("Deleted empty cache directory");
        }
    }
}
