using System.IO.Abstractions;
using Recyclarr.TrashLib.Settings;

namespace Recyclarr.TrashLib.Repo;

public interface IRepoUpdater
{
    Task UpdateRepo(IDirectoryInfo repoPath, IRepositorySettings repoSettings, CancellationToken token);
}
