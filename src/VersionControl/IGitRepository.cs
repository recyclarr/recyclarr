namespace VersionControl;

public interface IGitRepository : IDisposable
{
    void ForceCheckout(string branch);
    void Fetch(string remote = "origin");
    void ResetHard(string toBranch);
    void SetRemote(string name, string newUrl);
}
