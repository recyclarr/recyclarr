using System.IO.Abstractions;
using Recyclarr.Logging;
using Recyclarr.Platform;
using Recyclarr.Settings;
using Serilog.Context;

namespace Recyclarr.Repo;

public class TrashGuidesRepo(
    IRepoUpdater repoUpdater,
    IAppPaths paths,
    ISettings<TrashRepository> settings
) : ITrashGuidesRepo, IUpdateableRepo
{
    public IDirectoryInfo Path { get; } = paths.ReposDirectory.SubDirectory("trash-guides");

    public Task Update(CancellationToken token)
    {
        using var logScope = LogContext.PushProperty(LogProperty.Scope, "Trash Guides Repo");
        return repoUpdater.UpdateRepo(Path, settings.Value, token);
    }
}
