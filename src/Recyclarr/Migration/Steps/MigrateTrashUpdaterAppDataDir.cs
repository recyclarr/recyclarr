using System.IO.Abstractions;
using JetBrains.Annotations;

namespace Recyclarr.Migration.Steps;

/// <summary>
///     Rename `trash.yml` to `recyclarr.yml`.
/// </summary>
/// <remarks>
///     Implemented on 4/30/2022.
/// </remarks>
[UsedImplicitly]
public class MigrateTrashUpdaterAppDataDir : IMigrationStep
{
    private readonly IFileSystem _fileSystem;

    private readonly string _oldPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "trash-updater");

    // Do not use AppPaths class here since that may change yet again in the future and break this migration step.
    private readonly string _newPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "recyclarr");

    public int Order => 20;
    public string Description { get; }
    public IReadOnlyCollection<string> Remediation { get; }

    public MigrateTrashUpdaterAppDataDir(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
        Remediation = new[]
        {
            $"Check if `{_newPath}` already exists. If so, manually copy settings you want and then delete `{_oldPath}` to fix the error.",
            $"Ensure Recyclarr has permission to recursively delete {_oldPath}",
            $"Ensure Recyclarr has permission to create {_newPath}"
        };

        Description = $"Rename app data directory from `{_oldPath}` to `{_newPath}`";
    }

    public bool CheckIfNeeded() => _fileSystem.Directory.Exists(_oldPath);

    public void Execute()
    {
        _fileSystem.Directory.Move(_oldPath, _newPath);
    }
}
