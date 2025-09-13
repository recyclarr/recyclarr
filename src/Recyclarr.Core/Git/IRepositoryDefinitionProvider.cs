using Recyclarr.Settings.Models;

namespace Recyclarr.Git;

public interface IRepositoryDefinitionProvider
{
    string RepositoryType { get; }
    IEnumerable<GitRepositorySource> GetRepositoryDefinitions();
}
