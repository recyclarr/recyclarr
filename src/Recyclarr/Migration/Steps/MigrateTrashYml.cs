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
public class MigrateTrashYml : IMigrationStep
{
    private readonly IFileSystem _fileSystem;
    private readonly string _oldConfigPath = Path.Combine(AppContext.BaseDirectory, "trash.yml");

    // Do not use AppPaths class here since that may change yet again in the future and break this migration step.
    private readonly string _newConfigPath = Path.Combine(AppContext.BaseDirectory, "recyclarr.yml");

    public MigrateTrashYml(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public int Order => 10;

    public string Description => "Migration from 'trash.yml' to 'recyclarr.yml'";

    public bool CheckIfNeeded() => _fileSystem.File.Exists(_oldConfigPath);

    public void Execute(ILogger log)
    {
        try
        {
            _fileSystem.File.Move(_oldConfigPath, _newConfigPath);
            log.Information("Migration: Default configuration renamed from {Old} to {New}", _oldConfigPath,
                _newConfigPath);
        }
        catch (IOException)
        {
            throw new MigrationException(Description,
                "Unable to move due to IO Exception (does 'recyclarr.yml' already exist next to the executable?)");
        }
    }
}
