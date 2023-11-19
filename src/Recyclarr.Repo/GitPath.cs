using Recyclarr.Settings;
using Recyclarr.VersionControl;

namespace Recyclarr.Repo;

public class GitPath(ISettingsProvider settings) : IGitPath
{
    public static string Default => "git";
    public string Path => settings.Settings.GitPath ?? Default;
}
