using Recyclarr.TrashLib.Settings;

namespace Recyclarr.TrashLib.Repo.VersionControl;

public class GitPath : IGitPath
{
    private readonly ISettingsProvider _settings;

    public GitPath(ISettingsProvider settings)
    {
        _settings = settings;
    }

    public static string Default => "git";
    public string Path => _settings.Settings.Repository.GitPath ?? Default;
}
