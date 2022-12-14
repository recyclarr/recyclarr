using Recyclarr.Cli.Logging;
using Recyclarr.TrashLib.Config.Settings;
using Serilog;

namespace Recyclarr.Cli.Command.Setup;

public class JanitorCleanupTask : IBaseCommandSetupTask
{
    private readonly ILogJanitor _janitor;
    private readonly ILogger _log;
    private readonly ISettingsProvider _settingsProvider;

    public JanitorCleanupTask(ILogJanitor janitor, ILogger log, ISettingsProvider settingsProvider)
    {
        _janitor = janitor;
        _log = log;
        _settingsProvider = settingsProvider;
    }

    public void OnStart()
    {
        // No work to do for this event
    }

    public void OnFinish()
    {
        var maxFiles = _settingsProvider.Settings.LogJanitor.MaxFiles;
        _log.Debug("Cleaning up logs using max files of {MaxFiles}", maxFiles);
        _janitor.DeleteOldestLogFiles(maxFiles);
    }
}
