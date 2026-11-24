using System.IO.Abstractions;
using Recyclarr.Platform;

namespace Recyclarr.Migration.Steps;

/// <summary>
/// Moves v8 state out of a nested <c>state</c> folder. v8 kept state in <c>&lt;config&gt;/state</c>,
/// so a v8 volume reused as the v9 state directory holds it one level too deep. The rest of v8's
/// files on that volume (configs, logs, resources) are only reported, never deleted: the step
/// cannot prove those files are disposable.
/// </summary>
internal class FlattenNestedStateMigrationStep(IAppPaths paths) : IMigrationStep
{
    private static readonly string[] ServiceDirectories = ["sonarr", "radarr"];

    // What v8 created or read in its config root, which held the nested state folder. Only these
    // are reported: other entries belong to the volume or to other tools (lost+found, .DS_Store).
    // v8 is frozen, so this list is complete.
    private static readonly string[] V8RootEntries =
    [
        "cache",
        "configs",
        "includes",
        "logs",
        "recyclarr.yaml",
        "recyclarr.yml",
        "repo",
        "resources",
        "secrets.yaml",
        "secrets.yml",
        "settings.yaml",
        "settings.yml",
    ];

    private IDirectoryInfo StateDir => paths.StateDirectory;
    private IDirectoryInfo NestedDir => StateDir.SubDirectory("state");

    public string Description => "Move nested state folder to the state directory root";

    public IReadOnlyCollection<string> Remediation =>
        [
            $"Ensure Recyclarr has permission to read/write {StateDir}",
            $"Manually move the contents of {NestedDir} to {StateDir}, then delete {NestedDir}",
        ];

    public void Run(ILogger log)
    {
        if (!IsNestedLayout())
        {
            return;
        }

        var moved = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in NestedDir.EnumerateFileSystemInfos().ToList())
        {
            switch (entry)
            {
                case IDirectoryInfo dir:
                    dir.MoveTo(StateDir.SubDirectory(dir.Name).FullName);
                    break;
                case IFileInfo file:
                    file.MoveTo(StateDir.File(file.Name).FullName);
                    break;
            }

            moved.Add(entry.Name);
        }

        NestedDir.Delete();
        log.Information("Moved state from {Source} to {Target}", NestedDir.FullName, StateDir);

        var leftovers = StateDir
            .EnumerateFileSystemInfos()
            .Select(e => e.Name)
            .Where(name => !moved.Contains(name) && V8RootEntries.Contains(name))
            .Order(StringComparer.Ordinal)
            .ToList();

        if (leftovers.Count > 0)
        {
            log.Warning(
                "The state directory {StateDir} also holds files from an older layout that "
                    + "Recyclarr no longer uses; delete them if you do not need them: {Leftovers}",
                StateDir,
                leftovers
            );
        }
    }

    private bool IsNestedLayout()
    {
        if (IsSameDirectory(StateDir, paths.ConfigDirectory))
        {
            return false;
        }

        if (IsSameDirectory(StateDir, paths.DataDirectory))
        {
            return false;
        }

        return ServiceDirectories.Any(name => NestedDir.SubDirectory(name).Exists)
            && !ServiceDirectories.Any(name => StateDir.SubDirectory(name).Exists);
    }

    private static bool IsSameDirectory(IDirectoryInfo a, IDirectoryInfo b)
    {
        var path = a.FileSystem.Path;
        return path.TrimEndingDirectorySeparator(a.FullName)
            == path.TrimEndingDirectorySeparator(b.FullName);
    }
}
