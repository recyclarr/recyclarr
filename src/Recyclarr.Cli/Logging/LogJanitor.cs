using Recyclarr.Platform;

namespace Recyclarr.Cli.Logging;

public class LogJanitor(IAppPaths paths)
{
    public void DeleteOldestLogFiles(int numberOfNewestToKeep)
    {
        foreach (
            var file in paths
                .LogDirectory.GetFiles()
                .OrderByDescending(f => f.Name)
                .Skip(numberOfNewestToKeep)
        )
        {
            file.Delete();
        }
    }
}
