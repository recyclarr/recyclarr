using System.IO.Abstractions;
using Recyclarr.Platform;
using Recyclarr.Repo;
using Recyclarr.Settings.Models;

namespace Recyclarr.ResourceProviders.Git;

public abstract class BaseRepositoryDefinitionProvider(IAppPaths appPaths)
    : IRepositoryDefinitionProvider
{
    private IReadOnlyCollection<GitRepositorySource>? _repositoryDefinitions;

    public abstract string RepositoryType { get; }

    public IReadOnlyCollection<GitRepositorySource> RepositoryDefinitions =>
        _repositoryDefinitions ??= BuildRepositoryDefinitions();

    private List<GitRepositorySource> BuildRepositoryDefinitions()
    {
        var gitProviders = GetUserProviders()
            .Where(p => p.Type == RepositoryType && p is GitResourceProvider)
            .Cast<GitResourceProvider>()
            .ToList();

        if (!gitProviders.Any(p => p.ReplaceDefault))
        {
            gitProviders.Insert(0, CreateOfficialRepository());
        }

        return gitProviders.Select(ConvertToRepositorySource).ToList();
    }

    private GitRepositorySource ConvertToRepositorySource(GitResourceProvider gitProvider)
    {
        var repoPath = appPaths
            .ReposDirectory.SubDirectory(RepositoryType)
            .SubDirectory("git")
            .SubDirectory(gitProvider.Name);

        return new GitRepositorySource
        {
            Name = gitProvider.Name,
            CloneUrl = gitProvider.CloneUrl,
            Reference = gitProvider.Reference,
            Path = repoPath,
        };
    }

    protected abstract IReadOnlyCollection<ResourceProvider> GetUserProviders();
    protected abstract GitResourceProvider CreateOfficialRepository();
}
