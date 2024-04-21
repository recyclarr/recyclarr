using System.IO.Abstractions;
using JetBrains.Annotations;
using Recyclarr.Platform;
using Spectre.Console;

namespace Recyclarr.Cli.Migration.Steps;

[UsedImplicitly]
public class MoveOsxAppDataDotnet8(
    IAppPaths paths,
    IEnvironment env,
    IRuntimeInformation runtimeInfo,
    IFileSystem fs)
    : IMigrationStep
{
    public string Description => "Migrate OSX app data to 'Library/Application Support'";
    public IReadOnlyCollection<string> Remediation => new[]
    {
        $"Ensure Recyclarr has permission to move {OldAppDataDir} to {NewAppDataDir} and try again",
        $"Move {OldAppDataDir} to {NewAppDataDir} manually if Recyclarr can't do it"
    };

    public bool Required => true;

    private IDirectoryInfo OldAppDataDir => fs.DirectoryInfo
        .New(env.GetFolderPath(Environment.SpecialFolder.UserProfile))
        .SubDirectory(".config", AppPaths.DefaultAppDataDirectoryName);

    private IDirectoryInfo NewAppDataDir => paths.AppDataDirectory;

    public bool CheckIfNeeded()
    {
        return runtimeInfo.IsPlatformOsx() && OldAppDataDir.Exists;
    }

    public void Execute(IAnsiConsole? console)
    {
        NewAppDataDir.Create();
        OldAppDataDir.MoveTo(NewAppDataDir.FullName);
        console?.WriteLine($"Moved app settings dir from '{OldAppDataDir}' to '{NewAppDataDir}'");
    }
}
