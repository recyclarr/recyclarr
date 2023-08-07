namespace Recyclarr.TrashLib.Repo.VersionControl;

public interface IGitRepository : IDisposable
{
    Task ForceCheckout(string branch);
    Task Fetch(string remote = "origin");
    Task ResetHard(string toBranchOrSha1);
    Task SetRemote(string name, Uri newUrl);
    Task Clone(Uri cloneUrl, string? branch = null, int depth = 0);
    Task Status();
}
