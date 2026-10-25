using System.Diagnostics;
using System.IO.Abstractions;
using Recyclarr.Common.Extensions;
using Recyclarr.Platform;
using Recyclarr.Repo;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.ResourceProviders.Storage;
using Recyclarr.Settings.Models;
using Recyclarr.VersionControl;

namespace Recyclarr.Core.Tests.ResourceProviders;

// Runs real git against a local origin repository: update, mark-good, and revert depend on git's
// shallow-fetch and ref semantics, which a double cannot reproduce.
internal sealed class GitProviderLocationTest
{
    private readonly FileSystem _fs = new();
    private IDirectoryInfo _root = null!;
    private IDirectoryInfo _origin = null!;
    private GitProviderLocation _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _root = _fs.DirectoryInfo.New(Directory.CreateTempSubdirectory("recyclarr-git-").FullName);
        _origin = _root.SubDirectory("origin");
        _origin.Create();
        Git(_origin, "init", "-q", "-b", "master");

        var log = Substitute.For<ILogger>();
        var gitPath = Substitute.For<IGitPath>();
        gitPath.Path.Returns("git");
        IGitRepository RepoFactory(IDirectoryInfo dir) => new GitRepository(log, gitPath, dir);

        var data = _root.SubDirectory("data");
        _sut = new GitProviderLocation(
            new GitResourceProvider
            {
                Name = "official",
                Type = "trash-guides",
                CloneUrl = new Uri(_origin.FullName),
                Reference = "master",
            },
            Substitute.For<IProviderTypeStrategy>(),
            new AppPaths(data, data, data.SubDirectory("state")),
            new RepoUpdater(log, RepoFactory),
            RepoFactory,
            log
        );
    }

    [TearDown]
    public void TearDown()
    {
        _root.DeleteReadOnlyDirectory();
    }

    private Task<string> ReadCachedFile() =>
        _fs.File.ReadAllTextAsync(_sut.Roots.Single().File("cf.json").FullName);

    [Test]
    public async Task Update_reports_whether_the_origin_changed()
    {
        Commit("v1");
        (await _sut.UpdateAsync(CancellationToken.None)).Should().Be(SourceUpdateOutcome.Updated);
        (await _sut.UpdateAsync(CancellationToken.None)).Should().Be(SourceUpdateOutcome.Unchanged);

        Commit("v2");
        (await _sut.UpdateAsync(CancellationToken.None)).Should().Be(SourceUpdateOutcome.Updated);
        (await ReadCachedFile()).Should().Be("v2");
    }

    [Test]
    public async Task Revert_restores_the_data_marked_good()
    {
        Commit("v1");
        await _sut.UpdateAsync(CancellationToken.None);
        await _sut.MarkGoodAsync(CancellationToken.None);
        Commit("v2");
        Commit("v3");
        await _sut.UpdateAsync(CancellationToken.None);

        var reverted = await _sut.RevertToGoodAsync(CancellationToken.None);

        reverted.Should().BeTrue();
        (await ReadCachedFile()).Should().Be("v1");
    }

    [Test]
    public async Task Revert_is_impossible_after_the_cache_is_rebuilt()
    {
        Commit("v1");
        await _sut.UpdateAsync(CancellationToken.None);
        await _sut.MarkGoodAsync(CancellationToken.None);
        _sut.Roots.Single().DeleteReadOnlyDirectory();
        Commit("v2");
        await _sut.UpdateAsync(CancellationToken.None);

        var reverted = await _sut.RevertToGoodAsync(CancellationToken.None);

        reverted.Should().BeFalse();
        (await ReadCachedFile()).Should().Be("v2");
    }

    [Test]
    public async Task Unreachable_origin_keeps_the_cached_data()
    {
        Commit("v1");
        await _sut.UpdateAsync(CancellationToken.None);
        _origin.DeleteReadOnlyDirectory();

        var outcome = await _sut.UpdateAsync(CancellationToken.None);

        outcome.Should().Be(SourceUpdateOutcome.FailedUsingCache);
        (await ReadCachedFile()).Should().Be("v1");
    }

    [Test]
    public async Task Unreachable_origin_without_cache_leaves_no_data_behind()
    {
        _origin.DeleteReadOnlyDirectory();

        var act = () => _sut.UpdateAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _sut.Roots.Should().BeEmpty();
    }

    private void Commit(string content)
    {
        _fs.File.WriteAllText(_origin.File("cf.json").FullName, content);
        Git(_origin, "add", "cf.json");
        Git(
            _origin,
            "-c",
            "user.name=test",
            "-c",
            "user.email=test@test",
            "commit",
            "-q",
            "-m",
            content
        );
    }

    private static void Git(IDirectoryInfo workDir, params string[] args)
    {
        var info = new ProcessStartInfo("git") { WorkingDirectory = workDir.FullName };
        foreach (var arg in args)
        {
            info.ArgumentList.Add(arg);
        }

        using var process = Process.Start(info) ?? throw new InvalidOperationException("git");
        process.WaitForExit();
        process.ExitCode.Should().Be(0, $"git {string.Join(' ', args)} must succeed");
    }
}
