using System.IO.Abstractions;

namespace Recyclarr.TrashLib.Repo;

public interface IRepoUpdater
{
    IDirectoryInfo RepoPath { get; }
    Task UpdateRepo();
}
