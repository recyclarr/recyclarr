using LibGit2Sharp;

namespace VersionControl;

public sealed class GitRepository : IGitRepository
{
    private readonly Lazy<Repository> _repo;

    public GitRepository(string repoPath)
    {
        // Lazily construct the Repository object because it does too much work in its constructor
        // We want to keep our own constructor here as thin as possible for DI and testability.
        _repo = new Lazy<Repository>(() => new Repository(repoPath));
    }

    public void Dispose()
    {
        if (_repo.IsValueCreated)
        {
            _repo.Value.Dispose();
        }
    }

    public void ForceCheckout(string branch)
    {
        Commands.Checkout(_repo.Value, branch, new CheckoutOptions
        {
            CheckoutModifiers = CheckoutModifiers.Force
        });
    }

    public void Fetch(string remote = "origin")
    {
        var origin = _repo.Value.Network.Remotes[remote];
        Commands.Fetch(_repo.Value, origin.Name, origin.FetchRefSpecs.Select(s => s.Specification), null, "");
    }

    public void ResetHard(string toBranch)
    {
        var commit = _repo.Value.Branches[toBranch].Tip;
        _repo.Value.Reset(ResetMode.Hard, commit);
    }

    public void SetRemote(string name, string newUrl)
    {
        _repo.Value.Network.Remotes.Update(name, updater => updater.Url = newUrl);
    }
}
