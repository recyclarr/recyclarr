using Recyclarr.Client.V1;

namespace Recyclarr.Cli.Processors.Sync;

internal sealed class ConfigDiagnosticsLogger(ILogger log)
{
    public void Log(ConfigDiagnosticsResponse diagnostics)
    {
        foreach (var file in diagnostics.MissingConfigFiles ?? [])
        {
            log.Error("Config file not found: {File}", file);
        }

        foreach (var failure in diagnostics.ParseFailures ?? [])
        {
            log.Error(
                "Config parsing failed in {File}: {Message}",
                failure.FileName ?? "unknown",
                failure.Message
            );
        }

        foreach (var instance in diagnostics.UnknownInstances ?? [])
        {
            log.Error("Instance does not exist: {Instance}", instance);
        }

        if (
            diagnostics.UnknownInstances is { Count: > 0 }
            && diagnostics.AvailableInstances is { Count: > 0 } available
        )
        {
            log.Information("Available instances: {Instances}", available);
        }

        foreach (var instance in diagnostics.InvalidInstances ?? [])
        {
            foreach (var error in instance.Errors)
            {
                log.Error("Invalid instance {Instance}: {Message}", instance.InstanceName, error);
            }
        }

        foreach (var instance in diagnostics.DuplicateInstances ?? [])
        {
            log.Error("Duplicate instance: {Instance}", instance);
        }

        foreach (var group in diagnostics.SplitInstanceGroups ?? [])
        {
            log.Error(
                "Instances {Instances} share the same base URL: {BaseUrl}",
                group.InstanceNames,
                group.BaseUrl
            );
        }

        foreach (var message in diagnostics.DeprecationWarnings ?? [])
        {
            log.Warning("[DEPRECATED] {Message}", message);
        }
    }
}
