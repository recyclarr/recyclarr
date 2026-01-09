using System.IO.Abstractions;
using System.Text;
using CliWrap;

namespace Recyclarr.VersionControl;

public sealed class GitRepository(ILogger log, IGitPath gitPath, IDirectoryInfo workDir)
    : IGitRepository
{
    private Task RunGitCmd(CancellationToken token, params string[] args)
    {
        return RunGitCmd(args, token);
    }

    private async Task RunGitCmd(ICollection<string> args, CancellationToken token)
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

        log.Debug("Command Output: {Output}", output.ToString().Trim());

        if (result.ExitCode != 0)
        {
            throw new GitCmdException(result.ExitCode, error.ToString());
        }
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
        string reference,
        CancellationToken token,
        IReadOnlyList<string>? extraArgs = null
    )
    {
        var args = new List<string> { "fetch" };
        if (extraArgs is not null)
        {
            args.AddRange(extraArgs);
        }

        args.AddRange([cloneUrl.ToString(), reference]);
        await RunGitCmd(args, token);
    }

    public async Task Status(CancellationToken token)
    {
        await RunGitCmd(token, "status");
    }

    public async Task ResetHard(string toBranchOrSha1, CancellationToken token)
    {
        await RunGitCmd(token, "reset", "--hard", toBranchOrSha1);
    }
}
