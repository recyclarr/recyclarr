using System.IO.Abstractions;
using System.Text;
using CliWrap;
using Common;
using Serilog;

namespace TrashLib.Repo.VersionControl;

public class GitRepositoryFactory : IGitRepositoryFactory
{
    private readonly IFileUtilities _fileUtils;
    private readonly Func<string, IGitRepository> _repoFactory;
    private readonly IGitPath _gitPath;
    private readonly ILogger _log;

    public GitRepositoryFactory(
        IFileUtilities fileUtils,
        Func<string, IGitRepository> repoFactory,
        IGitPath gitPath,
        ILogger log)
    {
        _fileUtils = fileUtils;
        _repoFactory = repoFactory;
        _gitPath = gitPath;
        _log = log;
    }

    private async Task<(bool Succeeded, string OutputMsg, string ErrorMsg)> IsValid(IDirectoryInfo repoPath)
    {
        var output = new StringBuilder();
        var error = new StringBuilder();
        var result = await Cli.Wrap(_gitPath.Path)
            .WithArguments("status")
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(error))
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))
            .WithValidation(CommandResultValidation.None)
            .WithWorkingDirectory(repoPath.FullName)
            .ExecuteAsync();

        return (result.ExitCode == 0, output.ToString(), error.ToString());
    }

    public async Task<IGitRepository> CreateAndCloneIfNeeded(string repoUrl, string repoPath, string branch)
    {
        var repo = _repoFactory(repoPath);

        if (!repo.Path.Exists)
        {
            _log.Information("Cloning trash repository...");
            await repo.Clone(repoUrl, branch);
        }
        else
        {
            var validResult = await IsValid(repo.Path);
            if (!validResult.Succeeded)
            {
                _log.Information("Git repository is invalid; deleting and cloning again");
                _log.Debug("Validity Check Output: {Message}", validResult.OutputMsg.Trim());
                _log.Debug("Validity Check Error: {Message}", validResult.ErrorMsg.Trim());

                _fileUtils.DeleteReadOnlyDirectory(repo.Path.FullName);
                await repo.Clone(repoUrl, branch);
            }
        }

        _log.Debug("Remote 'origin' set to {Url}", repoUrl);
        await repo.SetRemote("origin", repoUrl);
        return repo;
    }
}
