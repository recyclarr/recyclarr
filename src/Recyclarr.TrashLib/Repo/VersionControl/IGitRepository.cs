using System.Diagnostics.CodeAnalysis;

namespace Recyclarr.TrashLib.Repo.VersionControl;

[SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification =
    "Doesn't mix well with `params` (which has to be at the end)")]
public interface IGitRepository : IDisposable
{
    Task ForceCheckout(CancellationToken token, string branch);
    Task Fetch(CancellationToken token, string remote = "origin");
    Task ResetHard(CancellationToken token, string toBranchOrSha1);
    Task SetRemote(CancellationToken token, string name, Uri newUrl);
    Task Clone(CancellationToken token, Uri cloneUrl, string? branch = null, int depth = 0);
    Task Status(CancellationToken token);
}
