using System.IO.Abstractions;
using Recyclarr.Common.Extensions;
using Recyclarr.Platform;
using Recyclarr.Settings;
using Serilog.Context;

namespace Recyclarr.Repo;

public class ConfigTemplatesRepo(IRepoUpdater repoUpdater, IAppPaths paths, ISettingsProvider settings)
    : IConfigTemplatesRepo, IUpdateableRepo
{
    public IDirectoryInfo Path { get; } = paths.ReposDirectory.SubDir("config-templates");

    public Task Update(CancellationToken token)
    {
        using var logScope = LogContext.PushProperty(LogProperty.Scope, "Config Templates Repo");
        return repoUpdater.UpdateRepo(Path, settings.Settings.Repositories.ConfigTemplates, token);
    }
}
