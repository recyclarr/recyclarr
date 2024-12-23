using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Abstractions;
using System.Text;
using CliWrap;

namespace Recyclarr.VersionControl;

[SuppressMessage(
    "Design",
    "CA1068:CancellationToken parameters must come last",
    Justification = "Doesn't mix well with `params` (which has to be at the end)"
)]
public sealed class GitRepository(ILogger log, IGitPath gitPath, IDirectoryInfo workDir)
    : IGitRepository
{
    private Task RunGitCmd(CancellationToken token, params string[] args)
    {
        return RunGitCmd(token, (ICollection<string>)args);
    }

    private async Task RunGitCmd(CancellationToken token, ICollection<string> args)
    {
        log.Debug("Executing git command with args: {Args}", args);

        var output = new StringBuilder();
        var error = new StringBuilder();

        log.Debug("Using working directory: {Dir}", workDir.FullName);
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

    public async Task ForceCheckout(CancellationToken token, string branch)
    {
        await RunGitCmd(token, "checkout", "-f", branch);
    }

    public async Task Fetch(CancellationToken token, string remote = "origin")
    {
        await RunGitCmd(token, "fetch", remote);
    }

    public async Task Status(CancellationToken token)
    {
        await RunGitCmd(token, "status");
    }

    public async Task ResetHard(CancellationToken token, string toBranchOrSha1)
    {
        await RunGitCmd(token, "reset", "--hard", toBranchOrSha1);
    }

    public async Task SetRemote(CancellationToken token, string name, Uri newUrl)
    {
        await RunGitCmd(token, "remote", "set-url", name, newUrl.ToString());
    }

    public async Task Clone(
        CancellationToken token,
        Uri cloneUrl,
        string? branch = null,
        int depth = 0
    )
    {
        var args = new List<string> { "clone" };
        if (branch is not null)
        {
            args.AddRange(["-b", branch]);
        }

        if (depth != 0)
        {
            args.AddRange(["--depth", depth.ToString(CultureInfo.InvariantCulture)]);
        }

        args.AddRange([cloneUrl.ToString(), "."]);
        await RunGitCmd(token, args);
    }
}
