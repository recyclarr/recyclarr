using Serilog;
using TrashLib.Startup;

namespace Recyclarr.Command.Setup;

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
        _paths.RepoDirectory.Create();
        _paths.CacheDirectory.Create();
        _paths.LogDirectory.Create();
    }

    public void OnFinish()
    {
    }
}
