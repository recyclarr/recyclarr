using System.IO.Abstractions;
using Recyclarr.Common.Extensions;
using Recyclarr.Platform;

namespace Recyclarr.Migration.Steps;

internal class DeleteRepoDirMigrationStep(IAppPaths paths) : IMigrationStep
{
    public string Description => "Delete old repo directory";
    public IReadOnlyCollection<string> Remediation =>
        [
            $"Ensure Recyclarr has permission to recursively delete {RepoDir}",
            $"Delete {RepoDir} manually if Recyclarr can't do it",
        ];

    private IDirectoryInfo RepoDir => paths.ConfigDirectory.SubDirectory("repo");

    public void Run(ILogger log)
    {
        if (!RepoDir.Exists)
        {
            return;
        }

        RepoDir.RecursivelyDeleteReadOnly();
        log.Information("Deleted repo dir: {RepoDir}", RepoDir.FullName);
    }
}
