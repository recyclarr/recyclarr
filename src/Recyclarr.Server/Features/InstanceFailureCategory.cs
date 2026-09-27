using Recyclarr.Sync.Results;

namespace Recyclarr.Server.Features;

// The wire form of OperationalFailure, shared by every endpoint that reports why a call to a
// Sonarr or Radarr instance failed.
internal enum InstanceFailureCategory
{
    ServiceUnavailable,
    ServiceUnauthenticated,
    ServiceUnauthorized,
    ServiceRateLimited,
    ServiceIncompatible,
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
