using System.Diagnostics.CodeAnalysis;

namespace Recyclarr.VersionControl;

[SuppressMessage(
    "Design",
    "CA1068:CancellationToken parameters must come last",
    Justification = "Doesn't mix well with `params` (which has to be at the end)"
)]
public interface IGitRepository : IDisposable
{
    Task Fetch(CancellationToken token, Uri cloneUrl, string reference, int depth = 0);
    Task ResetHard(CancellationToken token, string toBranchOrSha1);
    Task Clone(CancellationToken token, Uri cloneUrl, string reference, int depth = 0);
    Task Status(CancellationToken token);
}
