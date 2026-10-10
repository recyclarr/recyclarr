using System.ComponentModel;
using Recyclarr.Sync.Results;

namespace Recyclarr.Server.Features;

// The wire form of OperationalFailure, shared by every endpoint that reports why a call to a
// Sonarr or Radarr instance failed.
/// <summary>Why a call to a Sonarr or Radarr instance failed.</summary>
internal enum InstanceFailureCategory
{
    [Description("The instance could not be reached or returned a server error (5xx).")]
    ServiceUnavailable,

    [Description("The instance rejected the API key (401).")]
    ServiceUnauthenticated,

    [Description("The instance denied access (403).")]
    ServiceUnauthorized,

    [Description("The instance is limiting requests (429).")]
    ServiceRateLimited,

    [Description("Recyclarr does not support the instance's version.")]
    ServiceIncompatible,

    [Description("Recyclarr could not read or save its stored sync state for the instance.")]
    SyncStateUnavailable,
}

internal static class InstanceFailureCategoryMapping
{
    extension(OperationalFailure failure)
    {
        public InstanceFailureCategory ToCategory() =>
            failure switch
            {
                ServiceUnavailableFailure => InstanceFailureCategory.ServiceUnavailable,
                ServiceUnauthenticatedFailure => InstanceFailureCategory.ServiceUnauthenticated,
                ServiceUnauthorizedFailure => InstanceFailureCategory.ServiceUnauthorized,
                ServiceRateLimitedFailure => InstanceFailureCategory.ServiceRateLimited,
                ServiceIncompatibleFailure => InstanceFailureCategory.ServiceIncompatible,
                SyncStateUnavailableFailure => InstanceFailureCategory.SyncStateUnavailable,
                _ => throw new ArgumentOutOfRangeException(nameof(failure), failure, null),
            };
    }
}
