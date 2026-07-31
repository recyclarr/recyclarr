namespace Recyclarr.Sync.Results;

/// <summary>
/// An expected semantic condition explaining a pipeline decision, rejection, skip, or failure.
/// </summary>
public abstract record PipelineOutcome;

/// <summary>
/// A calculated resource difference, independent of whether persistence was attempted or succeeded.
/// </summary>
public abstract record ResourceDelta;

/// <summary>
/// The observed and desired values of one semantic value within a resource delta.
/// </summary>
public sealed record ValueDelta<T>(T Current, T Desired);
