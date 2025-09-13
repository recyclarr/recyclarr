using Recyclarr.Settings.Models;

namespace Recyclarr.ResourceProviders.Git;

public interface IRepositoryDefinitionProvider
{
    string RepositoryType { get; }
    IEnumerable<GitRepositorySource> GetRepositoryDefinitions();
}
