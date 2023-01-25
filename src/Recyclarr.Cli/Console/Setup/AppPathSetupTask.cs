using Recyclarr.TrashLib.Startup;

namespace Recyclarr.Cli.Console.Setup;

public class AppPathSetupTask : IBaseCommandSetupTask
{
    private readonly ILogger _log;
    private readonly IAppPaths _paths;

    public AppPathSetupTask(ILogger log, IAppPaths paths)
    {
        _log = log;
        _paths = paths;
    }

    public void OnStart()
    {
        _log.Debug("App Data Dir: {AppData}", _paths.AppDataDirectory);

        // Initialize other directories used throughout the application
        // Do not initialize the repo directory here; the GitRepositoryFactory handles that later.
        _paths.CacheDirectory.Create();
        _paths.LogDirectory.Create();
        _paths.ConfigsDirectory.Create();
    }

    public void OnFinish()
    {
        // No work to do for this event
    }
}
