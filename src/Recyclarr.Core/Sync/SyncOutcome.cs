namespace Recyclarr.Sync;

public enum SyncOutcomeScope
{
    InstanceBlocking,
    ResourceLocal,
}

public abstract record SyncOutcome(
    SyncDiagnosticLevel Level,
    SyncOutcomeScope Scope = SyncOutcomeScope.InstanceBlocking
);
