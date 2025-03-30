using Recyclarr.Settings;
using Recyclarr.Settings.Models;
using Recyclarr.VersionControl;

namespace Recyclarr.Repo;

public class GitPath(ISettings<RecyclarrSettings> settings) : IGitPath
{
    public static string Default => "git";
    public string Path => settings.Value.GitPath ?? Default;
}
