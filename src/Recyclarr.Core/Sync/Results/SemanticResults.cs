namespace Recyclarr.Sync.Results;

/// <summary>
/// An expected semantic condition explaining a pipeline decision, rejection, skip, or failure.
/// </summary>
public abstract record PipelineOutcome;

/// <summary>
/// An expected semantic condition found while planning one service instance.
/// </summary>
/// <remarks>
/// Planning outcomes belong to the instance because planning can validate relationships across
/// pipelines before any pipeline executes.
/// </remarks>
public abstract record PlanningOutcome;

/// <summary>
/// A planning condition that prevents the instance's pipelines from executing.
/// </summary>
public abstract record BlockingPlanningOutcome : PlanningOutcome;

/// <summary>
/// A calculated resource difference, independent of whether persistence was attempted or succeeded.
/// </summary>
public abstract record ResourceDelta;

/// <summary>
/// The observed and desired values of one semantic value within a resource delta.
/// </summary>
public sealed record ValueDelta<T>(T Current, T Desired);
