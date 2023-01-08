using System.IO.Abstractions;
using JetBrains.Annotations;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Startup;
using Spectre.Console;

namespace Recyclarr.Cli.Migration.Steps;

/// <remarks>
///     Implemented on 4/30/2022.
/// </remarks>
[UsedImplicitly]
public class MigrateTrashUpdaterAppDataDir : IMigrationStep
{
    private readonly IFileSystem _fs;
    private readonly IAppPaths _paths;

    public int Order => 20;
    public bool Required => true;

    public string Description
        => $"Merge files from old app data directory `{OldPath}` into `{NewPath}` and delete old directory";

    public IReadOnlyCollection<string> Remediation => new[]
    {
        $"Check if `{NewPath}` already exists. If so, manually copy all files from `{OldPath}` and delete it to fix the error.",
        $"Ensure Recyclarr has permission to recursively delete {OldPath}",
        $"Ensure Recyclarr has permission to create and move files into {NewPath}"
    };

    public MigrateTrashUpdaterAppDataDir(IFileSystem fs, IAppPaths paths)
    {
        _fs = fs;
        _paths = paths;
    }

    public IDirectoryInfo NewPath
    {
        get
        {
            // Will be something like `/home/user/.config/recyclarr`.
            var path = _paths.AppDataDirectory;
            path.Refresh();
            return path;
        }
    }

    public IDirectoryInfo OldPath => NewPath.Parent.SubDirectory("trash-updater");

    public bool CheckIfNeeded()
    {
        return OldPath.Exists;
    }

    public void Execute(IAnsiConsole? console)
    {
        MoveDirectory("cache", console);
        MoveFile("recyclarr.yml");
        MoveFile("settings.yml");

        if (OldPath.Exists)
        {
            OldPath.Delete(true);
        }
    }

    private void MoveDirectory(string directory, IAnsiConsole? console)
    {
        var oldPath = OldPath.SubDirectory(directory);
        if (oldPath.Exists)
        {
            _fs.MergeDirectory(
                oldPath,
                NewPath.SubDirectory(directory),
                console);
        }
    }

    private void MoveFile(string file)
    {
        var recyclarrYaml = OldPath.File(file);
        if (recyclarrYaml.Exists)
        {
            recyclarrYaml.MoveTo(NewPath.File(file).FullName);
        }
    }
}
