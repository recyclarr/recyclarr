using System.IO.Abstractions;
using Recyclarr.Common.Extensions;
using Recyclarr.Platform;

namespace Recyclarr.Cli.Migration.Steps;

[UsedImplicitly]
internal class DeleteRepoDirMigrationStep(IAppPaths paths) : IMigrationStep
{
    public string Description => "Delete old repo directory";
    public IReadOnlyCollection<string> Remediation =>
        [
            $"Ensure Recyclarr has permission to recursively delete {RepoDir}",
            $"Delete {RepoDir} manually if Recyclarr can't do it",
        ];

    public bool Required => false;
    private IDirectoryInfo RepoDir => paths.AppDataDirectory.SubDirectory("repo");

    public bool CheckIfNeeded()
    {
        return RepoDir.Exists;
    }

    public void Execute(ILogger log)
    {
        RepoDir.RecursivelyDeleteReadOnly();
        log.Information("Deleted repo dir: {RepoDir}", RepoDir.FullName);
    }
}
