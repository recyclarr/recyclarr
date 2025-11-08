using Recyclarr.Repo;

namespace Recyclarr.ResourceProviders.Git;

public interface IRepositoryDefinitionProvider
{
    string RepositoryType { get; }
    IReadOnlyCollection<GitRepositorySource> RepositoryDefinitions { get; }
}
