namespace Recyclarr.ResourceProviders.Storage;

public record ProviderProgress(
    string ProviderType,
    string ProviderName,
    ProviderStatus Status,
    string? ErrorMessage = null,
    int? TotalProviders = null
);

public enum ProviderStatus
{
    Starting,
    Processing,
    Completed,
    Failed,
}
