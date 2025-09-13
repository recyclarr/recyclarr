using Recyclarr.Settings.Models;

namespace Recyclarr.ResourceProviders.Git;

public abstract class BaseRepositoryDefinitionProvider : IRepositoryDefinitionProvider
{
    public abstract string RepositoryType { get; }

    public IEnumerable<GitRepositorySource> GetRepositoryDefinitions()
    {
        var userRepositories = GetUserRepositories().OfType<GitRepositorySource>().ToList();

        // Check if user explicitly configured an "official" repository
        var hasExplicitOfficial = userRepositories.Any(repo => repo.Name == "official");

        if (hasExplicitOfficial)
        {
            // User knows what they're doing - return only their explicit configuration
            return userRepositories;
        }

        // Add implicit official repository first for highest precedence
        var officialRepo = CreateOfficialRepository();
        return new[] { officialRepo }.Concat(userRepositories);
    }

    protected abstract IReadOnlyCollection<IUnderlyingResourceProvider> GetUserRepositories();
    protected abstract GitRepositorySource CreateOfficialRepository();
}
