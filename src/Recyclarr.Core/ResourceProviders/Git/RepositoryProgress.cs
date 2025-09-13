namespace Recyclarr.ResourceProviders.Git;

public record RepositoryProgress(
    string RepositoryType,
    string RepositoryName,
    RepositoryProgressStatus Status,
    string? ErrorMessage = null
);

public enum RepositoryProgressStatus
{
    Processing,
    Completed,
    Failed,
}
