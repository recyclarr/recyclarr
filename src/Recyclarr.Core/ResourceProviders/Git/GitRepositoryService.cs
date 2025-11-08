using Recyclarr.Logging;
using Recyclarr.Repo;
using Serilog.Context;

namespace Recyclarr.ResourceProviders.Git;

internal class GitRepositoryService(
    IEnumerable<IRepositoryDefinitionProvider> definitionProviders,
    IRepoUpdater repoUpdater
) : IGitRepositoryService
{
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

        var tasks = definitionProviders
            .SelectMany(provider =>
                provider.RepositoryDefinitions.Select(repo => new
                {
                    provider.RepositoryType,
                    Repository = repo,
                })
            )
            .Select(definition =>
                ProcessRepositoryAsync(
                    definition.RepositoryType,
                    definition.Repository,
                    progress,
                    ct
                )
            );

        await Task.WhenAll(tasks);
        _isInitialized = true;
    }

    private async Task ProcessRepositoryAsync(
        string repoType,
        GitRepositorySource repoSource,
        IProgress<RepositoryProgress>? progress,
        CancellationToken ct
    )
    {
        try
        {
            progress?.Report(
                new RepositoryProgress(
                    repoType,
                    repoSource.Name,
                    RepositoryProgressStatus.Processing
                )
            );

            var scopeName = $"{repoType} Repository ({repoSource.Name})";
            using var logScope = LogContext.PushProperty(LogProperty.Scope, scopeName);

            await repoUpdater.UpdateRepo(repoSource, ct);

            progress?.Report(
                new RepositoryProgress(
                    repoType,
                    repoSource.Name,
                    RepositoryProgressStatus.Completed
                )
            );
        }
        catch (Exception ex)
        {
            progress?.Report(
                new RepositoryProgress(
                    repoType,
                    repoSource.Name,
                    RepositoryProgressStatus.Failed,
                    ex.Message
                )
            );
            throw;
        }
    }
}
