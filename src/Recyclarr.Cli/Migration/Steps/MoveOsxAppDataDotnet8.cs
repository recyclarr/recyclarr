using System.IO.Abstractions;
using Recyclarr.Platform;

namespace Recyclarr.Cli.Migration.Steps;

[UsedImplicitly]
internal class MoveOsxAppDataDotnet8(
    IAppPaths paths,
    IEnvironment env,
    IRuntimeInformation runtimeInfo,
    IFileSystem fs
) : IMigrationStep
{
    public string Description => "Migrate OSX app data to 'Library/Application Support'";
    public IReadOnlyCollection<string> Remediation =>
        [
            $"Ensure Recyclarr has permission to move {OldAppDataDir} to {NewAppDataDir} and try again",
            $"Move {OldAppDataDir} to {NewAppDataDir} manually if Recyclarr can't do it",
        ];

    public bool Required => true;

    private IDirectoryInfo OldAppDataDir =>
        fs
            .DirectoryInfo.New(env.GetFolderPath(Environment.SpecialFolder.UserProfile))
            .SubDirectory(".config", AppPaths.DefaultAppDataDirectoryName);

    private IDirectoryInfo NewAppDataDir => paths.AppDataDirectory;

    public bool CheckIfNeeded()
    {
        return runtimeInfo.IsPlatformOsx() && OldAppDataDir.Exists;
    }

    public void Execute(ILogger log)
    {
        NewAppDataDir.Create();
        OldAppDataDir.MoveTo(NewAppDataDir.FullName);
        log.Information(
            "Moved app settings dir from {OldDir} to {NewDir}",
            OldAppDataDir,
            NewAppDataDir
        );
    }
}
