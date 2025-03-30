using System.IO.Abstractions;
using Recyclarr.Logging;
using Recyclarr.Platform;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;
using Serilog.Context;

namespace Recyclarr.Repo;

public class ConfigTemplatesRepo(
    IRepoUpdater repoUpdater,
    IAppPaths paths,
    ISettings<ConfigTemplateRepository> settings
) : IConfigTemplatesRepo, IUpdateableRepo
{
    public IDirectoryInfo Path { get; } = paths.ReposDirectory.SubDirectory("config-templates");

    public Task Update(CancellationToken token)
    {
        using var logScope = LogContext.PushProperty(LogProperty.Scope, "Config Templates Repo");
        return repoUpdater.UpdateRepo(Path, settings.Value, token);
    }
}
