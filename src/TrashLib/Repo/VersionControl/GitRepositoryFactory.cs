using System.IO.Abstractions;
using CliWrap;
using Common;

namespace TrashLib.Repo.VersionControl;

public class GitRepositoryFactory : IGitRepositoryFactory
{
    private readonly IFileUtilities _fileUtils;
    private readonly Func<string, IGitRepository> _repoFactory;
    private readonly IGitPath _gitPath;

    public GitRepositoryFactory(
        IFileUtilities fileUtils,
        Func<string, IGitRepository> repoFactory,
        IGitPath gitPath)
    {
        _fileUtils = fileUtils;
        _repoFactory = repoFactory;
        _gitPath = gitPath;
    }

    private async Task<bool> IsValid(IDirectoryInfo repoPath)
    {
        var result = await Cli.Wrap(_gitPath.Path)
            .WithArguments("status")
            .WithValidation(CommandResultValidation.None)
            .WithWorkingDirectory(repoPath.FullName)
            .ExecuteAsync();

        return result.ExitCode == 0;
    }

    public async Task<IGitRepository> CreateAndCloneIfNeeded(string repoUrl, string repoPath, string branch)
    {
        var repo = _repoFactory(repoPath);

        if (!await IsValid(repo.Path))
        {
            await DeleteAndCloneRepo(repo, repoUrl, branch);
        }

        await repo.SetRemote("origin", repoUrl);
        return repo;
    }

    private async Task DeleteAndCloneRepo(IGitRepository repo, string repoUrl, string branch)
    {
        _fileUtils.DeleteReadOnlyDirectory(repo.Path.FullName);
        await repo.Clone(repoUrl, branch);
    }
}
