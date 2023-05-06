using System.IO.Abstractions;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Settings;
using Recyclarr.TrashLib.Startup;

namespace Recyclarr.TrashLib.Repo;

public class TrashGuidesRepo : ITrashGuidesRepo
{
    private readonly IRepoUpdater _repoUpdater;
    private readonly ISettingsProvider _settings;

    public TrashGuidesRepo(IRepoUpdater repoUpdater, IAppPaths paths, ISettingsProvider settings)
    {
        _repoUpdater = repoUpdater;
        _settings = settings;
        Path = paths.ReposDirectory.SubDir("trash-guides");
    }

    public IDirectoryInfo Path { get; }

    public Task Update()
    {
        return _repoUpdater.UpdateRepo(Path, _settings.Settings.Repositories.TrashGuides);
    }
}
