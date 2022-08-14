namespace VersionControl;

public interface IGitRepository : IDisposable
{
    void ForceCheckout(string branch);
    void Fetch(string remote = "origin");
    void ResetHard(string toBranchOrSha1);
    void SetRemote(string name, string newUrl);
}
