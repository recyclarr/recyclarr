using Recyclarr.Platform;

namespace Recyclarr.Cli.Logging;

internal class LogJanitor(IAppPaths paths)
{
    public void DeleteOldestLogFiles(int numberOfNewestToKeep)
    {
        foreach (
            var file in paths
                .CliLogDirectory.GetFiles()
                .OrderByDescending(f => f.Name)
                .Skip(numberOfNewestToKeep)
        )
        {
            file.Delete();
        }
    }
}
