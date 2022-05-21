namespace Recyclarr.Command.Initialization;

internal class OldLogFileCleaner : IServiceCleaner
{
    private readonly ILogJanitor _janitor;

    public OldLogFileCleaner(ILogJanitor janitor)
    {
        _janitor = janitor;
    }

    public void Cleanup()
    {
        _janitor.DeleteOldestLogFiles(20);
    }
}
