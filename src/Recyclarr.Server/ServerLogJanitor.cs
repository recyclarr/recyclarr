using System.IO.Abstractions;
using Recyclarr.Platform;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.Server;

internal sealed class ServerLogJanitor(IAppPaths paths, ISettings<LogJanitorSettings> settings)
{
    public void DeleteOldestLogFiles(IFileInfo activeLogFile)
    {
        var numberOfNewestToKeep = settings.Value.MaxFiles;
        var completedFilesToKeep = Math.Max(0, numberOfNewestToKeep - 1);

        foreach (
            var file in paths
                .ServerLogDirectory.GetFiles()
                .Where(file =>
                    !file.FullName.Equals(activeLogFile.FullName, StringComparison.Ordinal)
                )
                .OrderByDescending(file => file.Name)
                .Skip(completedFilesToKeep)
        )
        {
            file.Delete();
        }
    }
}
