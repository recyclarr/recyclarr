namespace Recyclarr.Sync.Results;

/// <summary>
/// Summarizes how a terminal sync result concluded.
/// </summary>
public enum SyncResultStatus
{
    /// <summary>Every intended resource completed its required stage.</summary>
    Succeeded,

    /// <summary>At least one intended resource completed and at least one did not.</summary>
    Partial,

    /// <summary>No intended resource completed, or no usable result was produced.</summary>
    Failed,

    /// <summary>The pipeline did not run because a dependency was not successful.</summary>
    Blocked,
}

public static class SyncResultStatusExtensions
{
    extension(SyncResultStatus status)
    {
        /// <summary>Whether a pipeline with this status permits a dependent to run.</summary>
        public bool SatisfiesDependency() => status is SyncResultStatus.Succeeded;
    }
}
