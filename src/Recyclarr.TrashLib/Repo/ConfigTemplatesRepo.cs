using System.IO.Abstractions;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Settings;
using Recyclarr.TrashLib.Startup;

namespace Recyclarr.TrashLib.Repo;

public class ConfigTemplatesRepo : IConfigTemplatesRepo
{
    private readonly IRepoUpdater _repoUpdater;
    private readonly ISettingsProvider _settings;

    public ConfigTemplatesRepo(IRepoUpdater repoUpdater, IAppPaths paths, ISettingsProvider settings)
    {
        _repoUpdater = repoUpdater;
        _settings = settings;
        Path = paths.ReposDirectory.SubDir("config-templates");
    }

    public IDirectoryInfo Path { get; }

    public Task Update()
    {
        return _repoUpdater.UpdateRepo(Path, _settings.Settings.Repositories.ConfigTemplates);
    }
}
