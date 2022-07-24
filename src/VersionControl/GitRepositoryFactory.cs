using Common;
using LibGit2Sharp;
using VersionControl.Wrappers;

namespace VersionControl;

public class GitRepositoryFactory : IGitRepositoryFactory
{
    private readonly IFileUtilities _fileUtils;
    private readonly IRepositoryStaticWrapper _staticWrapper;
    private readonly Func<string, IGitRepository> _repoFactory;
    private readonly Func<ProgressBar> _progressBarFactory;

    public GitRepositoryFactory(
        IFileUtilities fileUtils,
        IRepositoryStaticWrapper staticWrapper,
        Func<string, IGitRepository> repoFactory,
        Func<ProgressBar> progressBarFactory)
    {
        _fileUtils = fileUtils;
        _staticWrapper = staticWrapper;
        _repoFactory = repoFactory;
        _progressBarFactory = progressBarFactory;
    }

    public IGitRepository CreateAndCloneIfNeeded(string repoUrl, string repoPath, string branch)
    {
        if (!_staticWrapper.IsValid(repoPath))
        {
            DeleteAndCloneRepo(repoUrl, repoPath, branch);
        }

        var repo = _repoFactory(repoPath);
        repo.SetRemote("origin", repoUrl);
        return repo;
    }

    private void DeleteAndCloneRepo(string repoUrl, string repoPath, string branch)
    {
        _fileUtils.DeleteReadOnlyDirectory(repoPath);

        var progress = _progressBarFactory();
        progress.Description = "Fetching guide data\n";

        _staticWrapper.Clone(repoUrl, repoPath, new CloneOptions
        {
            RecurseSubmodules = false,
            BranchName = branch,
            OnTransferProgress = gitProgress =>
            {
                progress.ReportProgress.OnNext((float) gitProgress.ReceivedObjects / gitProgress.TotalObjects);
                return true;
            }
        });
    }
}
