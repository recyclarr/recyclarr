using System.IO.Abstractions;
using System.Text;
using CliWrap;

namespace Recyclarr.TrashLib.Repo.VersionControl;

public sealed class GitRepository : IGitRepository
{
    private readonly ILogger _log;
    private readonly IGitPath _gitPath;
    private readonly IDirectoryInfo _workDir;

    public GitRepository(ILogger log, IGitPath gitPath, IDirectoryInfo workDir)
    {
        _log = log;
        _gitPath = gitPath;
        _workDir = workDir;
    }

    private Task RunGitCmd(params string[] args)
    {
        return RunGitCmd((ICollection<string>) args);
    }

    private async Task RunGitCmd(ICollection<string> args)
    {
        _log.Debug("Executing git command with args: {Args}", args);

        var output = new StringBuilder();
        var error = new StringBuilder();

        _log.Debug("Using working directory: {Dir}", _workDir.FullName);
        _workDir.Create();

        var cli = Cli.Wrap(_gitPath.Path)
            .WithArguments(args)
            .WithValidation(CommandResultValidation.None)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(error))
            .WithWorkingDirectory(_workDir.FullName);

        var result = await cli.ExecuteAsync();

        _log.Debug("Command Output: {Output}", output.ToString().Trim());

        if (result.ExitCode != 0)
        {
            throw new GitCmdException(result.ExitCode, error.ToString());
        }
    }

    public void Dispose()
    {
        // Nothing to do here
    }

    public async Task ForceCheckout(string branch)
    {
        await RunGitCmd("checkout", "-f", branch);
    }

    public async Task Fetch(string remote = "origin")
    {
        await RunGitCmd("fetch", remote);
    }

    public async Task Status()
    {
        await RunGitCmd("status");
    }

    public async Task ResetHard(string toBranchOrSha1)
    {
        await RunGitCmd("reset", "--hard", toBranchOrSha1);
    }

    public async Task SetRemote(string name, Uri newUrl)
    {
        await RunGitCmd("remote", "set-url", name, newUrl.ToString());
    }

    public async Task Clone(Uri cloneUrl, string? branch = null, int depth = 0)
    {
        var args = new List<string> {"clone"};
        if (branch is not null)
        {
            args.AddRange(new[] {"-b", branch});
        }

        if (depth != 0)
        {
            args.AddRange(new[] {"--depth", depth.ToString()});
        }

        args.AddRange(new[] {cloneUrl.ToString(), "."});
        await RunGitCmd(args);
    }
}
