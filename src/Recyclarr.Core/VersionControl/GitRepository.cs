using System.IO.Abstractions;
using System.Text;
using CliWrap;

namespace Recyclarr.VersionControl;

public sealed class GitRepository(ILogger log, IGitPath gitPath, IDirectoryInfo workDir)
    : IGitRepository
{
    private Task<string> RunGitCmd(CancellationToken token, params string[] args) =>
        RunGitCmdCore(args, token);

    private async Task<string> RunGitCmdCore(ICollection<string> args, CancellationToken token)
    {
        log.Debug("Executing git command with args: {Args}", args);

        var output = new StringBuilder();
        var error = new StringBuilder();

        workDir.Create();

        var cli = Cli.Wrap(gitPath.Path)
            .WithArguments(args)
            .WithValidation(CommandResultValidation.None)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(error))
            .WithWorkingDirectory(workDir.FullName);

        var result = await cli.ExecuteAsync(token);

        var outputStr = output.ToString().Trim();
        log.Debug("Command Output: {Output}", outputStr);

        if (result.ExitCode != 0)
        {
            throw new GitCmdException(result.ExitCode, error.ToString());
        }

        return outputStr;
    }

    public void Dispose()
    {
        // Nothing to do here
    }

    public async Task Init(CancellationToken token)
    {
        await RunGitCmd(token, "init");
    }

    public async Task Fetch(
        Uri cloneUrl,
        IReadOnlyList<string> references,
        CancellationToken token,
        IReadOnlyList<string>? extraArgs = null
    )
    {
        List<GitCmdException> errors = [];

        foreach (var reference in references)
        {
            try
            {
                log.Debug("Attempting to fetch reference: {Reference}", reference);
                await FetchReference(cloneUrl, reference, extraArgs, token);
                log.Debug("Successfully fetched reference: {Reference}", reference);
                return;
            }
            catch (GitCmdException e)
            {
                log.Debug("Failed to fetch reference {Reference}: {Error}", reference, e.Message);
                errors.Add(e);
            }
        }

        throw new AggregateException(
            $"All references failed to fetch: [{string.Join(", ", references)}]",
            errors
        );
    }

    private async Task FetchReference(
        Uri cloneUrl,
        string reference,
        IReadOnlyList<string>? extraArgs,
        CancellationToken token
    )
    {
        var args = new List<string> { "fetch" };
        if (extraArgs is not null)
        {
            args.AddRange(extraArgs);
        }

        args.AddRange([cloneUrl.ToString(), reference]);
        await RunGitCmdCore(args, token);
    }

    public async Task Status(CancellationToken token)
    {
        await RunGitCmd(token, "status");
    }

    public async Task ResetHard(string toBranchOrSha1, CancellationToken token)
    {
        await RunGitCmd(token, "reset", "--hard", toBranchOrSha1);
    }

    public async Task RunMaintenance(CancellationToken token)
    {
        // Trim .git/shallow to only the current HEAD so prior depth-1 fetches don't keep old
        // root commits reachable (git gc alone won't remove them while they appear in shallow).
        var headSha = await RunGitCmdCore(["rev-parse", "HEAD"], token);
        var shallowFile = workDir.SubDirectory(".git").File("shallow");
        await workDir.FileSystem.File.WriteAllTextAsync(
            shallowFile.FullName,
            headSha + "\n",
            token
        );

        // Expire all reflog entries so they don't pin unreachable objects.
        await RunGitCmd(token, "reflog", "expire", "--expire=now", "--all");

        // -A: unpack unreachable packed objects into loose objects so gc can prune them.
        await RunGitCmd(token, "repack", "-Ad");

        // Prune the now-loose unreachable objects.
        await RunGitCmd(token, "gc", "--prune=now");
    }
}
