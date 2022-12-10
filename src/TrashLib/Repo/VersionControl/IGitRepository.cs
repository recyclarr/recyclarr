using System.IO.Abstractions;

namespace TrashLib.Repo.VersionControl;

public interface IGitRepository : IDisposable
{
    Task ForceCheckout(string branch);
    Task Fetch(string remote = "origin");
    Task ResetHard(string toBranchOrSha1);
    Task SetRemote(string name, string newUrl);
    IDirectoryInfo Path { get; }
    Task Clone(string cloneUrl, string? branch = null);
    Task Status();
}
