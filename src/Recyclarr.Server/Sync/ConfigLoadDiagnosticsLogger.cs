namespace Recyclarr.Server.Sync;

/// <summary>
/// Writes config-load diagnostics to the log. Clients render the structured form for the user;
/// this is the log-channel counterpart, matching what DiagnosticsLogger does for the diagnostics a
/// sync run emits while it executes.
/// </summary>
internal static class ConfigLoadDiagnosticsLogger
{
    public static void Log(ILogger log, ConfigLoadDiagnostics diagnostics)
    {
        foreach (var file in diagnostics.MissingConfigFiles)
        {
            log.Error("Config file not found: {File}", file);
        }

        foreach (var failure in diagnostics.ParseFailures)
        {
            log.Error(
                "Config parsing failed in {File}: {Message}",
                failure.FileName ?? "unknown",
                failure.Message
            );
        }

        foreach (var instance in diagnostics.UnknownInstances)
        {
            log.Error("Instance does not exist: {Instance}", instance);
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

        foreach (var message in diagnostics.DeprecationWarnings)
        {
            log.Warning("[DEPRECATED] {Message}", message);
        }
    }
}
