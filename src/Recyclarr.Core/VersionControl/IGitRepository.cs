namespace Recyclarr.VersionControl;

public interface IGitRepository : IDisposable
{
    Task Init(CancellationToken token);

    Task Fetch(
        Uri cloneUrl,
        IReadOnlyList<string> references,
        CancellationToken token,
        IReadOnlyList<string>? extraArgs = null
    );

    Task ResetHard(string toBranchOrSha1, CancellationToken token);
    Task Status(CancellationToken token);

    /// <summary>
    /// The commit SHA that <paramref name="revision"/> names, or null when it names no commit.
    /// </summary>
    Task<string?> ResolveCommit(string revision, CancellationToken token);

    Task UpdateRef(string refName, string commit, CancellationToken token);
}
