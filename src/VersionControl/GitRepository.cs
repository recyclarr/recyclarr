using System.Linq;
using LibGit2Sharp;

namespace VersionControl;

public sealed class GitRepository : IGitRepository
{
    private readonly Repository _repo;

    public GitRepository(string repoPath)
    {
        _repo = new Repository(repoPath);
    }

    public void Dispose()
    {
        _repo.Dispose();
    }

    public void ForceCheckout(string branch)
    {
        Commands.Checkout(_repo, branch, new CheckoutOptions
        {
            CheckoutModifiers = CheckoutModifiers.Force
        });
    }

    public void Fetch(string remote = "origin")
    {
        var origin = _repo.Network.Remotes[remote];
        Commands.Fetch(_repo, origin.Name, origin.FetchRefSpecs.Select(s => s.Specification), null, "");
    }

    public void ResetHard(string toBranch)
    {
        var commit = _repo.Branches[toBranch].Tip;
        _repo.Reset(ResetMode.Hard, commit);
    }
}
