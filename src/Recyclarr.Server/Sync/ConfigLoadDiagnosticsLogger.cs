namespace Recyclarr.Server.Sync;

// Writes configuration diagnostics to the server log. The resource coordinator also publishes them
// as server diagnostics (ADR-035).
internal static class ConfigLoadDiagnosticsLogger
{
    public static void Log(ILogger log, ConfigLoadDiagnostics diagnostics)
    {
        foreach (var failure in diagnostics.ParseFailures)
        {
            log.Error(
                "Config parsing failed in {File}: {Message}",
                failure.FileName ?? "unknown",
                failure.Message
            );
        }

        foreach (var instance in diagnostics.InvalidInstances)
        {
            foreach (var error in instance.Errors)
            {
                log.Error("Invalid instance {Instance}: {Message}", instance.InstanceName, error);
            }
        }

        foreach (var instance in diagnostics.DuplicateInstances)
        {
            log.Error("Duplicate instance: {Instance}", instance);
        }

        foreach (var group in diagnostics.SplitInstanceGroups)
        {
            log.Error(
                "Instances {Instances} share the same base URL: {BaseUrl}",
                group.InstanceNames,
                group.BaseUrl
            );
        }

        foreach (var deprecation in diagnostics.DeprecationWarnings)
        {
            log.Warning("[DEPRECATED] {Message}", deprecation.Message);
        }
    }
}
