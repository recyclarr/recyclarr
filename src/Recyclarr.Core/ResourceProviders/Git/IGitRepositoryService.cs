namespace Recyclarr.ResourceProviders.Git;

public interface IGitRepositoryService
{
    Task InitializeAsync(
        IProgress<RepositoryProgress>? progress = null,
        CancellationToken ct = default
    );
}
