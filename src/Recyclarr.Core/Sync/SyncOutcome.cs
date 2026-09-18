namespace Recyclarr.Sync;

public enum SyncDiagnosticLevel
{
    Error,
    Warning,
    Deprecation,
}

public enum SyncOutcomeScope
{
    InstanceBlocking,
    ResourceLocal,
}

public abstract record SyncOutcome(
    SyncDiagnosticLevel Level,
    SyncOutcomeScope Scope = SyncOutcomeScope.InstanceBlocking
);
