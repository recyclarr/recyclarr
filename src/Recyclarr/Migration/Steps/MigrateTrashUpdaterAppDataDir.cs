using System.IO.Abstractions;
using CliFx.Infrastructure;
using Common.Extensions;
using JetBrains.Annotations;
using TrashLib;

namespace Recyclarr.Migration.Steps;

/// <remarks>
///     Implemented on 4/30/2022.
/// </remarks>
[UsedImplicitly]
public class MigrateTrashUpdaterAppDataDir : IMigrationStep
{
    private readonly IFileSystem _fs;
    private readonly Lazy<string> _newPath, _oldPath;

    public int Order => 20;
    public bool Required => true;

    public string Description
        => $"Merge files from old app data directory `{GetOldPath()}` into `{GetNewPath()}` and delete old directory";

    public IReadOnlyCollection<string> Remediation => new[]
    {
        $"Check if `{GetNewPath()}` already exists. If so, manually copy all files from `{GetOldPath()}` and delete it to fix the error.",
        $"Ensure Recyclarr has permission to recursively delete {GetOldPath()}",
        $"Ensure Recyclarr has permission to create and move files into {GetNewPath()}"
    };

    private string GetNewPath() => _newPath.Value;
    private string GetOldPath() => _oldPath.Value;

    public MigrateTrashUpdaterAppDataDir(IFileSystem fs, IAppPaths paths)
    {
        _fs = fs;

        // Will be something like `/home/user/.config/recyclarr`.
        _newPath = new Lazy<string>(paths.GetAppDataPath);
        _oldPath = new Lazy<string>(() => _fs.Path.Combine(_fs.Path.GetDirectoryName(GetNewPath()), "trash-updater"));
    }

    public bool CheckIfNeeded() => _fs.Directory.Exists(GetOldPath());

    public void Execute(IConsole? console) => _fs.MergeDirectory(GetOldPath(), GetNewPath(), console);
}
