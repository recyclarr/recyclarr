namespace Recyclarr.ResourceProviders.Storage;

public record ProviderProgress(
    string ProviderType,
    string ProviderName,
    ProviderStatus Status,
    string? ErrorMessage = null
);

public enum ProviderStatus
{
    Processing,
    Completed,
    Failed,
}
