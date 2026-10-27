using Microsoft.Extensions.Options;
using Recyclarr.Settings.Models;
using Recyclarr.VersionControl;

namespace Recyclarr.Repo;

public class GitPath(IOptions<RecyclarrSettings> settings) : IGitPath
{
    public static string Default => "git";
    public string Path => settings.Value.GitPath ?? Default;
}
