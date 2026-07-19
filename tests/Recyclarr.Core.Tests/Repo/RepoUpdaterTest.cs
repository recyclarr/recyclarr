using Recyclarr.Common;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Repo;
using Recyclarr.VersionControl;

namespace Recyclarr.Core.Tests.Repo;

internal sealed class RepoUpdaterTest
{
    [Test, AutoMockData]
    public async Task Maintenance_runs_when_git_dir_exceeds_cache_limit(
        [Frozen] IGitRepository repo,
        [Frozen] MockFileSystem fs
    )
    {
        // 200MB .git dir with 100MB limit
        var repoPath = fs.WithGitDir(200 * 1024 * 1024);
        repo.HasRemoteReferences(CancellationToken.None).ReturnsForAnyArgs(false);

        var source = new GitRepositorySource
        {
            Name = "test",
            CloneUrl = new Uri("https://example.com/repo.git"),
            References = ["master"],
            Path = repoPath,
            CacheLimit = DataSize.FromMegabytes(100),
        };

        var sut = new RepoUpdater(Substitute.For<ILogger>(), _ => repo);

        await sut.UpdateRepo(source, CancellationToken.None);

        await repo.ReceivedWithAnyArgs().RunMaintenance(default);
    }

    [Test, AutoMockData]
    public async Task Oversized_legacy_cache_is_reinitialized(
        [Frozen] IGitRepository repo,
        [Frozen] MockFileSystem fs
    )
    {
        var repoPath = fs.WithGitDir(200 * 1024 * 1024);
        repo.HasRemoteReferences(CancellationToken.None).ReturnsForAnyArgs(true);

        var source = new GitRepositorySource
        {
            Name = "test",
            CloneUrl = new Uri("https://example.com/repo.git"),
            References = ["master"],
            Path = repoPath,
            CacheLimit = DataSize.FromMegabytes(100),
        };

        var sut = new RepoUpdater(Substitute.For<ILogger>(), _ => repo);

        await sut.UpdateRepo(source, CancellationToken.None);

        await repo.ReceivedWithAnyArgs().HasRemoteReferences(default);
        await repo.ReceivedWithAnyArgs().Init(default);
        await repo.DidNotReceiveWithAnyArgs().RunMaintenance(default);
    }

    [Test, AutoMockData]
    public async Task Maintenance_skipped_when_git_dir_is_below_cache_limit(
        [Frozen] IGitRepository repo,
        [Frozen] MockFileSystem fs
    )
    {
        // 10MB .git dir with 100MB limit
        var repoPath = fs.WithGitDir(10 * 1024 * 1024);

        var source = new GitRepositorySource
        {
            Name = "test",
            CloneUrl = new Uri("https://example.com/repo.git"),
            References = ["master"],
            Path = repoPath,
            CacheLimit = DataSize.FromMegabytes(100),
        };

        var sut = new RepoUpdater(Substitute.For<ILogger>(), _ => repo);

        await sut.UpdateRepo(source, CancellationToken.None);

        await repo.DidNotReceiveWithAnyArgs().RunMaintenance(default);
    }

    [Test, AutoMockData]
    public async Task Maintenance_skipped_when_cache_limit_is_zero(
        [Frozen] IGitRepository repo,
        [Frozen] MockFileSystem fs
    )
    {
        // Large .git dir but limit is 0 (disabled)
        var repoPath = fs.WithGitDir(500 * 1024 * 1024);

        var source = new GitRepositorySource
        {
            Name = "test",
            CloneUrl = new Uri("https://example.com/repo.git"),
            References = ["master"],
            Path = repoPath,
            CacheLimit = DataSize.FromMegabytes(0),
        };

        var sut = new RepoUpdater(Substitute.For<ILogger>(), _ => repo);

        await sut.UpdateRepo(source, CancellationToken.None);

        await repo.DidNotReceiveWithAnyArgs().RunMaintenance(default);
    }

    [Test, AutoMockData]
    public async Task Maintenance_failure_does_not_fail_the_sync(
        [Frozen] IGitRepository repo,
        [Frozen] MockFileSystem fs
    )
    {
        var repoPath = fs.WithGitDir(200 * 1024 * 1024);
        repo.HasRemoteReferences(CancellationToken.None).ReturnsForAnyArgs(false);
        repo.RunMaintenance(CancellationToken.None)
            .ReturnsForAnyArgs(_ => throw new GitCmdException(1, "gc failed"));

        var source = new GitRepositorySource
        {
            Name = "test",
            CloneUrl = new Uri("https://example.com/repo.git"),
            References = ["master"],
            Path = repoPath,
            CacheLimit = DataSize.FromMegabytes(100),
        };

        var sut = new RepoUpdater(Substitute.For<ILogger>(), _ => repo);

        var act = () => sut.UpdateRepo(source, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
