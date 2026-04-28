using System.IO.Abstractions;
using Recyclarr.Common;
using Recyclarr.Repo;
using Recyclarr.VersionControl;

namespace Recyclarr.Core.Tests.Repo;

internal sealed class RepoUpdaterTest
{
    private static MockFileSystem CreateFsWithGitDir(IDirectoryInfo repoPath, long gitDirSizeBytes)
    {
        var fs = (MockFileSystem)repoPath.FileSystem;

        // Create required .git structure so validation passes
        fs.AddEmptyFile(fs.Path.Combine(repoPath.FullName, ".git/config"));
        fs.AddEmptyFile(fs.Path.Combine(repoPath.FullName, ".git/index"));
        fs.AddEmptyFile(fs.Path.Combine(repoPath.FullName, ".git/HEAD"));

        // Add a file under .git to simulate the accumulated size
        if (gitDirSizeBytes > 0)
        {
            var packFile = fs.Path.Combine(repoPath.FullName, ".git/objects/pack/pack-abc.pack");
            fs.AddFile(packFile, new MockFileData(new byte[gitDirSizeBytes]));
        }

        return fs;
    }

    [Test, AutoMockData]
    public async Task Maintenance_runs_when_git_dir_exceeds_cache_limit(
        [Frozen] IGitRepository repo,
        [Frozen] MockFileSystem fs
    )
    {
        var repoPath = fs.CurrentDirectory().SubDirectory("repo");

        // 200MB .git dir with 100MB limit
        CreateFsWithGitDir(repoPath, 200 * 1024 * 1024);

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
    public async Task Maintenance_skipped_when_git_dir_is_below_cache_limit(
        [Frozen] IGitRepository repo,
        [Frozen] MockFileSystem fs
    )
    {
        var repoPath = fs.CurrentDirectory().SubDirectory("repo");

        // 10MB .git dir with 100MB limit
        CreateFsWithGitDir(repoPath, 10 * 1024 * 1024);

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
        var repoPath = fs.CurrentDirectory().SubDirectory("repo");

        // Large .git dir but limit is 0 (disabled)
        CreateFsWithGitDir(repoPath, 500 * 1024 * 1024);

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
        var repoPath = fs.CurrentDirectory().SubDirectory("repo");

        CreateFsWithGitDir(repoPath, 200 * 1024 * 1024);

        repo.RunMaintenance(default!)
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
