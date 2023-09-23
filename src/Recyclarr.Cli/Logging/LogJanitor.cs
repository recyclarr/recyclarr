using Recyclarr.Platform;

namespace Recyclarr.Cli.Logging;

public class LogJanitor : ILogJanitor
{
    private readonly IAppPaths _paths;

    public LogJanitor(IAppPaths paths)
    {
        _paths = paths;
    }

    public void DeleteOldestLogFiles(int numberOfNewestToKeep)
    {
        foreach (var file in _paths.LogDirectory.GetFiles()
            .OrderByDescending(f => f.Name)
            .Skip(numberOfNewestToKeep))
        {
            file.Delete();
        }
    }
}
