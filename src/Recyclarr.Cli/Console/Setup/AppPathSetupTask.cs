using Recyclarr.Platform;

namespace Recyclarr.Cli.Console.Setup;

public class AppPathSetupTask(ILogger log, IAppPaths paths) : IBaseCommandSetupTask
{
    public void OnStart()
    {
        log.Debug("App Data Dir: {AppData}", paths.AppDataDirectory);

        // Initialize other directories used throughout the application
        // Do not initialize the repo directory here; the GitRepositoryFactory handles that later.
        paths.CacheDirectory.Create();
        paths.LogDirectory.Create();
        paths.ConfigsDirectory.Create();
        paths.IncludesDirectory.Create();
    }

    public void OnFinish()
    {
        // No work to do for this event
    }
}
