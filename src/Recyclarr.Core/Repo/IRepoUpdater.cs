using System.IO.Abstractions;
using Recyclarr.Settings.Models;

namespace Recyclarr.Repo;

public interface IRepoUpdater
{
    Task UpdateRepo(
        IDirectoryInfo repoPath,
        GitRepositorySource repositorySource,
        CancellationToken token
    );
}
