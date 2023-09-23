using System.IO.Abstractions;
using Recyclarr.Common.Extensions;
using Recyclarr.Platform;
using Recyclarr.Settings;
using Serilog.Context;

namespace Recyclarr.Repo;

public class ConfigTemplatesRepo : IConfigTemplatesRepo, IUpdateableRepo
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

    public Task Update(CancellationToken token)
    {
        using var logScope = LogContext.PushProperty(LogProperty.Scope, "Config Templates Repo");
        return _repoUpdater.UpdateRepo(Path, _settings.Settings.Repositories.ConfigTemplates, token);
    }
}
