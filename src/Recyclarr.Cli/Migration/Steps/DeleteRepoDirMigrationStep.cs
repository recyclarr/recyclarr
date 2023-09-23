using System.IO.Abstractions;
using JetBrains.Annotations;
using Recyclarr.Common.Extensions;
using Recyclarr.Platform;
using Spectre.Console;

namespace Recyclarr.Cli.Migration.Steps;

[UsedImplicitly]
public class DeleteRepoDirMigrationStep : IMigrationStep
{
    private readonly IAppPaths _paths;

    public DeleteRepoDirMigrationStep(IAppPaths paths)
    {
        _paths = paths;
    }

    public int Order => 1;
    public string Description => "Delete old repo directory";
    public IReadOnlyCollection<string> Remediation => new[]
    {
        $"Ensure Recyclarr has permission to recursively delete {RepoDir}",
        $"Delete {RepoDir} manually if Recyclarr can't do it"
    };

    public bool Required => false;
    private IDirectoryInfo RepoDir => _paths.AppDataDirectory.SubDir("repo");

    public bool CheckIfNeeded()
    {
        return RepoDir.Exists;
    }

    public void Execute(IAnsiConsole? console)
    {
        RepoDir.RecursivelyDeleteReadOnly();
        console?.WriteLine($"Deleted repo dir: {RepoDir.FullName}");
    }
}
