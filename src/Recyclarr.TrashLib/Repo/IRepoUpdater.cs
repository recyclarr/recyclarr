using System.IO.Abstractions;
using Recyclarr.TrashLib.Settings;

namespace Recyclarr.TrashLib.Repo;

public interface IRepoUpdater
{
    IDirectoryInfo RepoPath { get; }
    Task UpdateRepo(IRepositorySettings repoSettings);
}
