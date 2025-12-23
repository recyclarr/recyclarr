using Recyclarr.Common.Extensions;
using Recyclarr.VersionControl;

namespace Recyclarr.Repo;

public class RepoUpdater(ILogger log, IGitRepositoryFactory repositoryFactory) : IRepoUpdater
{
    public async Task UpdateRepo(GitRepositorySource repositorySource, CancellationToken token)
    {
        // Assume failure until it succeeds, to simplify the catch handlers.
        var succeeded = false;

        // Retry only once if there's a failure. This gives us an opportunity to delete the git repository and start
        // fresh.
        try
        {
            await CheckoutAndUpdateRepo(repositorySource, token);
            succeeded = true;
        }
        catch (GitCmdException e)
        {
            log.Debug(e, "Non-zero exit code {ExitCode} while executing Git command", e.ExitCode);
        }
        catch (InvalidGitRepoException e)
        {
            log.Debug(e, "Git repository is not valid (missing files in `.git` directory)");
        }

        if (!succeeded)
        {
            log.Warning("Deleting local git repo and retrying git operation due to error");
            repositorySource.Path.DeleteReadOnlyDirectory();
            await CheckoutAndUpdateRepo(repositorySource, token);
        }
    }

    private async Task CheckoutAndUpdateRepo(
        GitRepositorySource repositorySource,
        CancellationToken token
    )
    {
        var cloneUrl = repositorySource.CloneUrl;
        var reference = repositorySource.Reference;
        var depth = repositorySource.Depth;

        log.Debug("Using URL: {Url}, Ref: {Reference}, Depth: {Depth}", cloneUrl, reference, depth);

        using var repo = await repositoryFactory.CreateAndCloneIfNeeded(
            cloneUrl,
            repositorySource.Path,
            reference,
            depth,
            token
        );
        try
        {
            await repo.Fetch(token, cloneUrl, reference, depth);
        }
        catch (GitCmdException e)
        {
            log.Warning(
                e,
                "Non-zero exit code {ExitCode} while running git fetch (will proceed with existing files)",
                e.ExitCode
            );
        }

        await repo.ResetHard(token, "FETCH_HEAD");
    }
}
