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

    public async Task Fetch(CancellationToken token, Uri cloneUrl, string reference, int depth = 0)
    {
        var args = new List<string> { "fetch" };
        if (depth != 0)
        {
            args.AddRange(["--depth", depth.ToString(CultureInfo.InvariantCulture)]);
        }

        args.AddRange([cloneUrl.ToString(), reference]);
        await RunGitCmd(token, args);
    }

    public async Task Status(CancellationToken token)
    {
        await RunGitCmd(token, "status");
    }

    public async Task ResetHard(CancellationToken token, string toBranchOrSha1)
    {
        await RunGitCmd(token, "reset", "--hard", toBranchOrSha1);
    }

    public async Task Clone(CancellationToken token, Uri cloneUrl, string reference, int depth = 0)
    {
        // Use init + fetch approach for uniform handling of branches and SHAs with depth
        await RunGitCmd(token, "init");

        var args = new List<string> { "fetch" };
        if (depth != 0)
        {
            args.AddRange(["--depth", depth.ToString(CultureInfo.InvariantCulture)]);
        }

        args.AddRange([cloneUrl.ToString(), reference]);
        await RunGitCmd(token, args);

        await RunGitCmd(token, "reset", "--hard", "FETCH_HEAD");
    }
}
