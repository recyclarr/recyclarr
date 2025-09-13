namespace Recyclarr.Git;

public record RepositoryProgress(
    string RepositoryType,
    string RepositoryName,
    RepositoryProgressStatus Status,
    string? ErrorMessage = null
);

public enum RepositoryProgressStatus
{
    Starting,
    Cloning,
    Updating,
    Completed,
    Failed,
}
