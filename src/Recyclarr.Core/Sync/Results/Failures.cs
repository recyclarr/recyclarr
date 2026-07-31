namespace Recyclarr.Sync.Results;

/// <summary>
/// An expected operational failure that stops an instance without exposing presentation details.
/// </summary>
public abstract record OperationalFailure;

public sealed record ServiceUnavailableFailure : OperationalFailure;

public sealed record ServiceUnauthenticatedFailure : OperationalFailure;

public sealed record ServiceUnauthorizedFailure : OperationalFailure;

public sealed record ServiceRateLimitedFailure : OperationalFailure;

/// <summary>
/// The connected service does not match the configuration or supported capabilities.
/// </summary>
public sealed record ServiceIncompatibleFailure : OperationalFailure;

/// <summary>
/// The instance stopped because its persisted synchronization state was unavailable.
/// </summary>
public sealed record SyncStateUnavailableFailure : OperationalFailure;

/// <summary>
/// An opaque reference to an unexpected run-level failure recorded outside the result contract.
/// </summary>
public sealed record SyncFault
{
    public SyncFault(string reference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        Reference = reference;
    }

    public string Reference { get; }
}
