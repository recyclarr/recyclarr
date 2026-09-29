using Recyclarr.Client.V1;

namespace Recyclarr.Cli.ErrorHandling;

/// <summary>
/// User-facing text for the failure category the server reports when a Sonarr or Radarr instance
/// could not complete its work. Shared by request failures (HTTP 502) and sync job results so both
/// describe the same category the same way.
/// </summary>
internal static class ServiceFailureText
{
    public static string Describe(InstanceFailureCategory? failure) =>
        failure switch
        {
            InstanceFailureCategory.ServiceUnauthenticated =>
                "The service rejected the API key (401). Check api_key in your configuration.",
            InstanceFailureCategory.ServiceUnauthorized =>
                "The service denied access (403). Check the API key's permissions.",
            InstanceFailureCategory.ServiceRateLimited =>
                "The service is rate limiting requests (429). Try again later.",
            InstanceFailureCategory.ServiceIncompatible =>
                "The service version is not supported by Recyclarr.",
            InstanceFailureCategory.SyncStateUnavailable =>
                "Recyclarr's saved sync state for this instance is unavailable.",
            _ => "The service could not be reached. Check base_url and that the service is up.",
        };
}
