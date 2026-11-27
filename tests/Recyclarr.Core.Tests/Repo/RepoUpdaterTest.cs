using System.IO.Abstractions;
using Recyclarr.Common;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Repo;
using Recyclarr.VersionControl;

namespace Recyclarr.Core.Tests.Repo;

internal sealed class RepoUpdaterTest
{
    private readonly IGitRepository _repo = Substitute.For<IGitRepository>();
    private readonly MockFileSystem _fs = new();

    [After(Test)]
    public void DisposeRepo() => _repo.Dispose();

    private static GitRepositorySource NewSource(IDirectoryInfo repoPath, int cacheLimitMb) =>
        new()
        {
            Name = "test",
            CloneUrl = new Uri("https://example.com/repo.git"),
            References = ["master"],
            Path = repoPath,
            CacheLimit = DataSize.FromMegabytes(cacheLimitMb),
        };

    private static bool LoggedWarningContaining(ILogger log, string fragment) =>
        log.ReceivedCalls()
            .Any(c =>
                c.GetMethodInfo().Name == nameof(ILogger.Warning)
                && c.GetArguments()
                    .Any(a => a?.ToString()?.Contains(fragment, StringComparison.Ordinal) == true)
            );

    [Test]
    public async Task Cache_deleted_for_rebuild_when_git_dir_exceeds_cache_limit()
    {
        // 200MB .git dir with 100MB limit
        var repoPath = _fs.WithGitDir(200 * 1024 * 1024);
        var source = NewSource(repoPath, cacheLimitMb: 100);

        var sut = new RepoUpdater(Substitute.For<ILogger>(), _ => _repo);
        await sut.UpdateRepo(source, CancellationToken.None);

        // Mocked git never recreates the directory, so its absence proves the delete happened
        _fs.Directory.Exists(repoPath.FullName)
            .Should()
            .BeFalse("cache exceeded the limit; it should be deleted for rebuild");
        await _repo.Received().Init(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Cache_kept_when_git_dir_is_below_cache_limit()
    {
        // 10MB .git dir with 100MB limit
        var repoPath = _fs.WithGitDir(10 * 1024 * 1024);
        var source = NewSource(repoPath, cacheLimitMb: 100);

        var sut = new RepoUpdater(Substitute.For<ILogger>(), _ => _repo);
        await sut.UpdateRepo(source, CancellationToken.None);

        _fs.Directory.Exists(repoPath.FullName)
            .Should()
            .BeTrue("cache is within the limit; no rebuild expected");
        await _repo.DidNotReceive().Init(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Cache_kept_when_cache_limit_is_zero()
    {
        // Large .git dir but limit is 0 (disabled)
        var repoPath = _fs.WithGitDir(500 * 1024 * 1024);
        var source = NewSource(repoPath, cacheLimitMb: 0);

        var sut = new RepoUpdater(Substitute.For<ILogger>(), _ => _repo);
        await sut.UpdateRepo(source, CancellationToken.None);

        _fs.Directory.Exists(repoPath.FullName)
            .Should()
            .BeTrue("cache_limit=0 disables the check entirely");
        await _repo.DidNotReceive().Init(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Delete_failure_proceeds_with_existing_cache()
    {
        // 200MB .git dir with 100MB limit, but the pack file is locked so deletion fails
        var repoPath = _fs.WithGitDir(200 * 1024 * 1024);
        var packFile = _fs.Path.Combine(repoPath.FullName, ".git/objects/pack/pack-abc.pack");
        _fs.GetFile(packFile).AllowedFileShare = FileShare.None;

        var source = NewSource(repoPath, cacheLimitMb: 100);

        var sut = new RepoUpdater(Substitute.For<ILogger>(), _ => _repo);
        var act = () => sut.UpdateRepo(source, CancellationToken.None);

        await act.Should().NotThrowAsync();
        _fs.Directory.Exists(repoPath.FullName)
            .Should()
            .BeTrue("delete failed; sync proceeds with the existing cache");
        await _repo.Received().Status(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Failed_fetch_falls_back_to_cached_data()
    {
        var repoPath = _fs.WithGitDir(10 * 1024 * 1024);
        var log = Substitute.For<ILogger>();
        var source = NewSource(repoPath, cacheLimitMb: 0);

        _repo
            .Fetch(default!, default!, default)
            .ReturnsForAnyArgs(Task.FromException(new AggregateException("offline")));
        _repo.ResolveCommit("HEAD", default).ReturnsForAnyArgs("abc123");

        var sut = new RepoUpdater(log, _ => _repo);
        var outcome = await sut.UpdateRepo(source, CancellationToken.None);

        outcome.Should().Be(RepoUpdateOutcome.FetchFailedUsingCache);
        _fs.Directory.Exists(repoPath.FullName).Should().BeTrue();
        LoggedWarningContaining(log, "will proceed with existing files").Should().BeTrue();
    }

    [Test]
    public async Task Warning_logged_when_rebuilt_cache_still_exceeds_limit()
    {
        // 200MB .git dir with 100MB limit; the "fresh" fetch recreates a 150MB cache
        var repoPath = _fs.WithGitDir(200 * 1024 * 1024);
        var log = Substitute.For<ILogger>();
        var source = NewSource(repoPath, cacheLimitMb: 100);

        _repo
            .WhenForAnyArgs(r => r.Fetch(default!, default!, default))
            .Do(_ => _fs.WithGitDir(150 * 1024 * 1024));

        var sut = new RepoUpdater(log, _ => _repo);
        await sut.UpdateRepo(source, CancellationToken.None);

        LoggedWarningContaining(log, "still exceeds")
            .Should()
            .BeTrue("fresh cache exceeds the limit; user must be told the limit is unachievable");
    }
}
