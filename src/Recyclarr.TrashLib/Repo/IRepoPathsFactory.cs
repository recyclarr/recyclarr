namespace Recyclarr.TrashLib.Repo;

public interface IRepoPathsFactory
{
    IRepoPaths Create();
    RepoMetadata Metadata { get; }
}
