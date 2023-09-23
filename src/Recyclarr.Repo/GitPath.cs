using Recyclarr.Settings;
using Recyclarr.VersionControl;

namespace Recyclarr.Repo;

public class GitPath : IGitPath
{
    private readonly ISettingsProvider _settings;

    public GitPath(ISettingsProvider settings)
    {
        _settings = settings;
    }

    public static string Default => "git";
    public string Path => _settings.Settings.GitPath ?? Default;
}
