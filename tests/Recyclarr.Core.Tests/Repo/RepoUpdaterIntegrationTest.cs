using System.IO.Abstractions;
using System.Text;
using CliWrap;
using Recyclarr.Common;
using Recyclarr.Common.Extensions;
using Recyclarr.Repo;
using Recyclarr.VersionControl;

namespace Recyclarr.Core.Tests.Repo;

internal sealed class RepoUpdaterIntegrationTest
{
    [Test]
    public async Task Oversized_legacy_cache_is_rebuilt_without_stale_remote_history()
    {
        var fs = new FileSystem();
        var root = fs.DirectoryInfo.New(
            Path.Join(Path.GetTempPath(), $"recyclarr-git-test-{Guid.NewGuid():N}")
        );
        root.Create();

        try
        {
            var ct = CancellationToken.None;
            var sourcePath = root.SubDirectory("source");
            sourcePath.Create();
            await RunGit(sourcePath, ["init", "--initial-branch=master"], ct);

            await sourcePath.FileSystem.File.WriteAllTextAsync(
                sourcePath.File("guide.txt").FullName,
                "legacy",
                ct
            );
            await CommitAll(sourcePath, "legacy", ct);
            var legacyCommit = await RunGit(sourcePath, ["rev-parse", "HEAD"], ct);
            await RunGit(sourcePath, ["branch", "legacy"], ct);

            await sourcePath.FileSystem.File.WriteAllTextAsync(
                sourcePath.File("guide.txt").FullName,
                "current",
                ct
            );
            await CommitAll(sourcePath, "current", ct);
            var expectedHead = await RunGit(sourcePath, ["rev-parse", "HEAD"], ct);

            var cachePath = root.SubDirectory("cache");
            await RunGit(root, ["clone", sourcePath.FullName, cachePath.FullName], ct);

            var gitPath = Substitute.For<IGitPath>();
            gitPath.Path.Returns("git");
            var log = Substitute.For<ILogger>();
            var updater = new RepoUpdater(log, path => new GitRepository(log, gitPath, path));
            var source = new GitRepositorySource
            {
                Name = "test",
                CloneUrl = new Uri(sourcePath.FullName),
                References = ["master"],
                Path = cachePath,
                CacheLimit = DataSize.FromKilobytes(1),
            };

            await updater.UpdateRepo(source, ct);

            var remoteRefs = await RunGit(
                cachePath,
                ["for-each-ref", "--format=%(refname)", "refs/remotes"],
                ct
            );
            var actualHead = await RunGit(cachePath, ["rev-parse", "HEAD"], ct);
            var legacyCommitExists = await GitObjectExists(cachePath, legacyCommit, ct);

            remoteRefs.Should().BeEmpty();
            actualHead.Should().Be(expectedHead);
            legacyCommitExists.Should().BeFalse();
        }
        finally
        {
            root.DeleteReadOnlyDirectory();
        }
    }

    private static async Task CommitAll(
        IDirectoryInfo repository,
        string message,
        CancellationToken ct
    )
    {
        await RunGit(repository, ["add", "."], ct);
        await RunGit(
            repository,
            [
                "-c",
                "user.name=Recyclarr Tests",
                "-c",
                "user.email=recyclarr@example.com",
                "commit",
                "-m",
                message,
            ],
            ct
        );
    }

    private static async Task<bool> GitObjectExists(
        IDirectoryInfo repository,
        string objectId,
        CancellationToken ct
    )
    {
        var result = await ExecuteGit(repository, ["cat-file", "-e", objectId], ct);
        return result.ExitCode == 0;
    }

    private static async Task<string> RunGit(
        IDirectoryInfo repository,
        IReadOnlyCollection<string> arguments,
        CancellationToken ct
    )
    {
        var result = await ExecuteGit(repository, arguments, ct);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"git {string.Join(' ', arguments)} failed with exit code {result.ExitCode}: {result.Error}"
            );
        }

        return result.Output;
    }

    private static async Task<GitCommandResult> ExecuteGit(
        IDirectoryInfo repository,
        IReadOnlyCollection<string> arguments,
        CancellationToken ct
    )
    {
        var output = new StringBuilder();
        var error = new StringBuilder();

        var result = await Cli.Wrap("git")
            .WithArguments(arguments)
            .WithValidation(CommandResultValidation.None)
            .WithWorkingDirectory(repository.FullName)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(error))
            .ExecuteAsync(ct);

        return new GitCommandResult(
            result.ExitCode,
            output.ToString().Trim(),
            error.ToString().Trim()
        );
    }

    private sealed record GitCommandResult(int ExitCode, string Output, string Error);
}
