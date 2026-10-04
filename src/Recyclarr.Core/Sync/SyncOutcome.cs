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

/// <summary>
/// A diagnostic produced during sync. Each outcome type fixes its own level and scope; they are
/// facts about the kind of outcome, not per-instance data.
/// </summary>
public abstract record SyncOutcome
{
    public abstract SyncDiagnosticLevel Level { get; }
    public virtual SyncOutcomeScope Scope => SyncOutcomeScope.InstanceBlocking;
}
