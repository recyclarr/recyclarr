using Recyclarr.Logging;
using Serilog;
using TrashLib.Config.Settings;

namespace Recyclarr.Command.Setup;

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
    }

    public void OnFinish()
    {
        var maxFiles = _settingsProvider.Settings.LogJanitor.MaxFiles;
        _log.Debug("Cleaning up logs using max files of {MaxFiles}", maxFiles);
        _janitor.DeleteOldestLogFiles(maxFiles);
    }
}
