using System.IO.Abstractions;
using System.Text;
using CliWrap;
using Serilog;
using TrashLib.Startup;

namespace TrashLib.Repo.VersionControl;

public sealed class GitRepository : IGitRepository
{
    private readonly ILogger _log;
    private readonly IAppPaths _paths;
    private readonly IGitPath _gitPath;

    public GitRepository(ILogger log, IAppPaths paths, IGitPath gitPath)
    {
        _log = log;
        _paths = paths;
        _gitPath = gitPath;
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

        var cli = Cli.Wrap(_gitPath.Path)
            .WithArguments(args)
            .WithValidation(CommandResultValidation.None)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(error));

        if (_paths.RepoDirectory.Exists)
        {
            var workDir = _paths.RepoDirectory.FullName;
            _log.Debug("Using working directory: {Dir}", workDir);
            cli = cli.WithWorkingDirectory(workDir);
        }

        var result = await cli.ExecuteAsync();

        _log.Debug("Command Output: {Output}", output.ToString().Trim());

        if (result.ExitCode != 0)
        {
            throw new GitCmdException(result.ExitCode, error.ToString());
        }
    }

    public IDirectoryInfo Path => _paths.RepoDirectory;

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

    public async Task SetRemote(string name, string newUrl)
    {
        await RunGitCmd("remote", "set-url", name, newUrl);
    }

    public async Task Clone(string cloneUrl, string? branch = null)
    {
        var args = new List<string> {"clone"};
        if (branch is not null)
        {
            args.AddRange(new[] {"-b", branch});
        }

        args.AddRange(new[] {cloneUrl, _paths.RepoDirectory.FullName});
        await RunGitCmd(args);
    }
}
