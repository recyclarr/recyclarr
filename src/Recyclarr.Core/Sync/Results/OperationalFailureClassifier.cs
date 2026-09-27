using System.Net;
using Recyclarr.Compatibility;
using Recyclarr.SyncState;
using Refit;

namespace Recyclarr.Sync.Results;

/// <summary>
/// Maps exceptions from calls to a Sonarr or Radarr instance to the expected
/// <see cref="OperationalFailure"/> they represent. Sync and any other code that calls a service
/// share this mapping so callers report the same failure for the same cause.
/// </summary>
public static class OperationalFailureClassifier
{
    /// <summary>
    /// Returns the failure for an expected service or sync-state problem, or <c>null</c> for an
    /// unexpected exception that the caller should treat as a fault.
    /// </summary>
    public static OperationalFailure? Classify(Exception exception) =>
        exception switch
        {
            ServiceIncompatibilityException => new ServiceIncompatibleFailure(),
            SyncStateUnavailableException => new SyncStateUnavailableFailure(),
            ApiRequestException => new ServiceUnavailableFailure(),
            HttpRequestException => new ServiceUnavailableFailure(),
            ApiException { StatusCode: HttpStatusCode.Unauthorized } =>
                new ServiceUnauthenticatedFailure(),
            ApiException { StatusCode: HttpStatusCode.Forbidden } =>
                new ServiceUnauthorizedFailure(),
            ApiException { StatusCode: HttpStatusCode.TooManyRequests } =>
                new ServiceRateLimitedFailure(),
            ApiException { StatusCode: >= HttpStatusCode.InternalServerError } =>
                new ServiceUnavailableFailure(),
            _ => null,
        };
}
