using System.IO.Abstractions;
using Microsoft.Extensions.Options;
using Recyclarr.Platform;
using Recyclarr.Settings.Models;

namespace Recyclarr.Server.Logging;

internal sealed class ServerLogJanitor(IAppPaths paths, IOptions<LogJanitorSettings> settings)
{
    public void DeleteOldestLogFiles(IReadOnlyCollection<IFileInfo> activeLogFiles)
    {
        var activePaths = activeLogFiles.Select(file => file.FullName).ToHashSet();
        var completedFilesToKeep = Math.Max(0, settings.Value.MaxFiles - activePaths.Count);

        foreach (
            var file in paths
                .ServerLogDirectory.GetFiles()
                .Where(file => !activePaths.Contains(file.FullName))
                .OrderByDescending(file => file.Name)
                .Skip(completedFilesToKeep)
        )
        {
            file.Delete();
        }
    }
}
