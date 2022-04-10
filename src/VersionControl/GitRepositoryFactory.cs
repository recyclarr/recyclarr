using Common;
using LibGit2Sharp;
using VersionControl.Wrappers;

namespace VersionControl;

public class GitRepositoryFactory : IGitRepositoryFactory
{
    private readonly IFileUtilities _fileUtils;
    private readonly IRepositoryStaticWrapper _staticWrapper;
    private readonly Func<string, IGitRepository> _repoFactory;

    public GitRepositoryFactory(
        IFileUtilities fileUtils,
        IRepositoryStaticWrapper staticWrapper,
        Func<string, IGitRepository> repoFactory)
    {
        _fileUtils = fileUtils;
        _staticWrapper = staticWrapper;
        _repoFactory = repoFactory;
    }

    public IGitRepository CreateAndCloneIfNeeded(string repoUrl, string repoPath, string branch)
    {
        if (!_staticWrapper.IsValid(repoPath))
        {
            DeleteAndCloneRepo(repoUrl, repoPath, branch);
        }

        return _repoFactory(repoPath);
    }

    private void DeleteAndCloneRepo(string repoUrl, string repoPath, string branch)
    {
        _fileUtils.DeleteReadOnlyDirectory(repoPath);

        var progress = new ProgressBar
        {
            Description = "Fetching guide data"
        };

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
