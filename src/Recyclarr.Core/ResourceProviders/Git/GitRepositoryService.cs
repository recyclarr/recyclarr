using System.IO.Abstractions;
using Recyclarr.Logging;
using Recyclarr.Platform;
using Recyclarr.Repo;
using Serilog.Context;

namespace Recyclarr.ResourceProviders.Git;

public interface IGitRepositoryService
{
    Task InitializeAsync(
        IProgress<RepositoryProgress>? progress = null,
        CancellationToken ct = default
    );
    IEnumerable<IDirectoryInfo> GetRepositoriesOfType(string repositoryType);
}

internal class GitRepositoryService(
    IEnumerable<IRepositoryDefinitionProvider> definitionProviders,
    IRepoUpdater repoUpdater,
    IAppPaths appPaths
) : IGitRepositoryService
{
    private readonly Dictionary<string, List<IDirectoryInfo>> _repositoriesByType = new();
    private bool _isInitialized;

    public async Task InitializeAsync(
        IProgress<RepositoryProgress>? progress = null,
        CancellationToken ct = default
    )
    {
        if (_isInitialized)
        {
            return;
        }

        _repositoriesByType.Clear();

        var allRepositoryDefinitions = definitionProviders
            .SelectMany(provider =>
                provider
                    .GetRepositoryDefinitions()
                    .Select(repo => new { provider.RepositoryType, Repository = repo })
            )
            .ToList();

        // Clean up legacy repositories for each type
        foreach (var repositoryType in definitionProviders.Select(p => p.RepositoryType).Distinct())
        {
            var repoParentPath = appPaths.ReposDirectory.SubDirectory(repositoryType);
            LegacyRepositoryCleanup.CleanLegacyRepository(repoParentPath);
        }

        // Process all repositories in parallel
        var tasks = allRepositoryDefinitions.Select(async definition =>
        {
            var repoType = definition.RepositoryType;
            var gitRepo = definition.Repository;

            try
            {
                progress?.Report(
                    new RepositoryProgress(
                        repoType,
                        gitRepo.Name,
                        RepositoryProgressStatus.Processing
                    )
                );

                var repoParentPath = appPaths.ReposDirectory.SubDirectory(repoType);
                var repoPath = repoParentPath.SubDirectory(gitRepo.Name);

                var scopeName = $"{repoType} Repository ({gitRepo.Name})";
                using var logScope = LogContext.PushProperty(LogProperty.Scope, scopeName);
                await repoUpdater.UpdateRepo(repoPath, gitRepo, ct);

                lock (_repositoriesByType)
                {
                    if (!_repositoriesByType.TryGetValue(repoType, out var repos))
                    {
                        repos = [];
                        _repositoriesByType[repoType] = repos;
                    }
                    repos.Add(repoPath);
                }

                progress?.Report(
                    new RepositoryProgress(
                        repoType,
                        gitRepo.Name,
                        RepositoryProgressStatus.Completed
                    )
                );
                return repoPath;
            }
            catch (Exception ex)
            {
                progress?.Report(
                    new RepositoryProgress(
                        repoType,
                        gitRepo.Name,
                        RepositoryProgressStatus.Failed,
                        ex.Message
                    )
                );
                throw;
            }
        });

        await Task.WhenAll(tasks);
        _isInitialized = true;
    }

    public IEnumerable<IDirectoryInfo> GetRepositoriesOfType(string repositoryType)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException(
                "GitRepositoryService has not been initialized. Call InitializeAsync first."
            );
        }

        return _repositoriesByType.TryGetValue(repositoryType, out var repositories)
            ? repositories
            : [];
    }
}
