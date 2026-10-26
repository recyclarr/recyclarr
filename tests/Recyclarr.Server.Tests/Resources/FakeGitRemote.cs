using System.Collections.Concurrent;
using System.Globalization;
using System.IO.Abstractions;
using Recyclarr.Repo;
using Recyclarr.VersionControl;

namespace Recyclarr.Server.Tests.Resources;

/// <summary>
/// Stands in for git at the updater and repository seams so coordinator tests control what each
/// fetch delivers. Each provider type (the cache's grandparent directory) has its own commit
/// history; a commit is the complete set of files in the repository. Git's own fetch and ref
/// semantics are covered against real git by GitProviderLocationTest.
/// </summary>
internal sealed class FakeGitRemote(MockFileSystem fs) : IRepoUpdater
{
    private readonly Dictionary<string, List<IReadOnlyDictionary<string, string>>> _commits = [];
    private readonly Dictionary<string, int> _heads = [];
    private readonly Dictionary<string, int> _good = [];
    private readonly Dictionary<string, int> _fetches = [];
    private readonly ConcurrentDictionary<string, TaskCompletionSource> _fetchWaiters = [];

    public bool Offline { get; set; }

    /// <summary>
    /// Completes when the next fetch of <paramref name="type"/> starts, so a test can follow a
    /// refresh that runs on another thread.
    /// </summary>
    public Task NextFetch(string type)
    {
        var waiter = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _fetchWaiters[type] = waiter;
        return waiter.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    public void Commit(string type, IReadOnlyDictionary<string, string> files)
    {
        if (!_commits.TryGetValue(type, out var history))
        {
            _commits[type] = history = [];
        }

        history.Add(files);
    }

    public int Fetches(string type) => _fetches.GetValueOrDefault(type);

    // What a repair (delete and re-clone) does to the good mark.
    public void ForgetGoodMarks() => _good.Clear();

    public Task<RepoUpdateOutcome> UpdateRepo(GitRepositorySource source, CancellationToken token)
    {
        var type = TypeOf(source.Path);
        _fetches[type] = Fetches(type) + 1;
        if (_fetchWaiters.TryRemove(type, out var waiter))
        {
            waiter.SetResult();
        }

        if (Offline)
        {
            return _heads.ContainsKey(type)
                ? Task.FromResult(RepoUpdateOutcome.FetchFailedUsingCache)
                : throw new InvalidOperationException("offline with no cached data");
        }

        Checkout(source.Path, _commits[type].Count - 1);
        return Task.FromResult(RepoUpdateOutcome.Fetched);
    }

    private static string TypeOf(IDirectoryInfo repo) => repo.Parent!.Parent!.Name;

    private void Checkout(IDirectoryInfo repo, int commit)
    {
        var type = TypeOf(repo);
        if (repo.Exists)
        {
            repo.Delete(recursive: true);
        }

        fs.AddDirectory(repo.SubDirectory(".git").FullName);
        foreach (var (path, content) in _commits[type][commit])
        {
            fs.AddFile(repo.File(path), new MockFileData(content));
        }

        _heads[type] = commit;
    }

    /// <summary>The local repository view of one cache directory.</summary>
    public sealed class Repository(FakeGitRemote remote, IDirectoryInfo workDir) : IGitRepository
    {
        private const string GoodRef = "refs/recyclarr/last-good";
        private string Type => TypeOf(workDir);

        public Task<string?> ResolveCommit(string revision, CancellationToken token)
        {
            var commits = revision == GoodRef ? remote._good : remote._heads;
            return Task.FromResult(
                commits.TryGetValue(Type, out var commit)
                    ? commit.ToString(CultureInfo.InvariantCulture)
                    : null
            );
        }

        public Task UpdateRef(string refName, string commit, CancellationToken token)
        {
            remote._good[Type] = int.Parse(commit, CultureInfo.InvariantCulture);
            return Task.CompletedTask;
        }

        public Task ResetHard(string toBranchOrSha1, CancellationToken token)
        {
            remote.Checkout(workDir, int.Parse(toBranchOrSha1, CultureInfo.InvariantCulture));
            return Task.CompletedTask;
        }

        public Task Init(CancellationToken token) => Task.CompletedTask;

        public Task Fetch(
            Uri cloneUrl,
            IReadOnlyList<string> references,
            CancellationToken token,
            IReadOnlyList<string>? extraArgs = null
        ) => Task.CompletedTask;

        public Task Status(CancellationToken token) => Task.CompletedTask;

        public void Dispose() { }
    }
}
