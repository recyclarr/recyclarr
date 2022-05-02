using System.IO.Abstractions;
using JetBrains.Annotations;
using Serilog;

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

    public MigrateTrashUpdaterAppDataDir(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public int Order => 20;

    public string Description => "Rename app data directory from 'trash-updater' to 'recyclarr'";

    public bool CheckIfNeeded() => _fileSystem.Directory.Exists(_oldPath);

    public void Execute(ILogger log)
    {
        try
        {
            _fileSystem.Directory.Move(_oldPath, _newPath);
            log.Information("Migration: App data directory renamed from {Old} to {New}", _oldPath, _newPath);
        }
        catch (IOException)
        {
            throw new MigrationException(Description,
                $"Unable to move due to IO Exception (does the '${_newPath}' directory already exist?)");
        }
    }
}
