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

    private async Task RunGitCmd(string args)
    {
        _log.Debug("Executing command: git {Args}", args);

        var output = new StringBuilder();
        var error = new StringBuilder();

        var result = await Cli.Wrap(_gitPath.Path)
            .WithArguments(args)
            .WithValidation(CommandResultValidation.None)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(error))
            .WithWorkingDirectory(_paths.RepoDirectory.FullName)
            .ExecuteAsync();

        _log.Debug("{Output}", output.ToString());

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
        await RunGitCmd($"checkout -f {branch}");
    }

    public async Task Fetch(string remote = "origin")
    {
        await RunGitCmd($"fetch {remote}");
    }

    public async Task ResetHard(string toBranchOrSha1)
    {
        await RunGitCmd($"reset --hard {toBranchOrSha1}");
    }

    public async Task SetRemote(string name, string newUrl)
    {
        await RunGitCmd($"remote set-url {name} {newUrl}");
    }

    public async Task Clone(string cloneUrl, string? branch = null)
    {
        var args = new StringBuilder("clone");
        if (branch is not null)
        {
            args.Append($" -b {branch}");
        }

        _paths.RepoDirectory.Create();
        args.Append($" {cloneUrl} .");
        await RunGitCmd(args.ToString());
    }
}
