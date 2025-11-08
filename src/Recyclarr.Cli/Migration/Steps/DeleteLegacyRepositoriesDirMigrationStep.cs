using System.IO.Abstractions;
using Recyclarr.Common.Extensions;
using Recyclarr.Platform;
using Spectre.Console;

namespace Recyclarr.Cli.Migration.Steps;

[UsedImplicitly]
internal class DeleteLegacyRepositoriesDirMigrationStep(IAppPaths paths) : IMigrationStep
{
    public string Description => "Delete legacy repositories directory";
    public IReadOnlyCollection<string> Remediation =>
        [
            $"Ensure Recyclarr has permission to recursively delete {LegacyReposDir}",
            $"Delete {LegacyReposDir} manually if Recyclarr can't do it",
        ];

    public bool Required => false;
    private IDirectoryInfo LegacyReposDir => paths.AppDataDirectory.SubDirectory("repositories");

    public bool CheckIfNeeded()
    {
        return LegacyReposDir.Exists;
    }

    public void Execute(IAnsiConsole? console)
    {
        LegacyReposDir.RecursivelyDeleteReadOnly();
        console?.WriteLine($"Deleted legacy repositories directory: {LegacyReposDir.FullName}");
    }
}
