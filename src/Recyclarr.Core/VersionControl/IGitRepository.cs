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
}
