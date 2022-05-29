using System.IO.Abstractions;
using CliFx.Infrastructure;
using JetBrains.Annotations;

namespace Recyclarr.Migration.Steps;

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

    public int Order => 10;
    public string Description { get; }
    public IReadOnlyCollection<string> Remediation { get; }
    public bool Required => true;

    public MigrateTrashYml(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
        Remediation = new[]
        {
            $"Check if `{_newConfigPath}` already exists. If so, manually copy the data you want and then delete `{_oldConfigPath}` to fix the error.",
            $"Ensure Recyclarr has permission to delete {_oldConfigPath}",
            $"Ensure Recyclarr has permission to create {_newConfigPath}"
        };

        Description = $"Rename default YAML config from `{_oldConfigPath}` to `{_newConfigPath}`";
    }

    public bool CheckIfNeeded() => _fileSystem.File.Exists(_oldConfigPath);

    public void Execute(IConsole? console)
    {
        _fileSystem.File.Move(_oldConfigPath, _newConfigPath);
    }
}
