using System.IO.Abstractions;
using Recyclarr.Settings;

namespace Recyclarr.Repo;

public interface IRepoUpdater
{
    Task UpdateRepo(
        IDirectoryInfo repoPath,
        IRepositorySettings repoSettings,
        CancellationToken token
    );
}
