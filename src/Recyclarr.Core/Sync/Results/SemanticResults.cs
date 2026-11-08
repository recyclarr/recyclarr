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
/// What the sync does to one managed resource.
/// </summary>
public enum PlannedAction
{
    Create,

    /// <summary>At least one managed value of the existing resource changes.</summary>
    Update,

    /// <summary>Every managed value already matches.</summary>
    Unchanged,
}

/// <summary>
/// One resource the sync manages, with every value it manages, changed or not. Calculated before
/// persistence, so it does not show whether a write was attempted or succeeded.
/// </summary>
public abstract record PlannedResource
{
    public required PlannedAction Action { get; init; }
}

/// <summary>
/// One managed value: <see cref="Desired"/> is the value after the sync. <see cref="Current"/> is
/// the value in the service, absent when the sync creates the resource. <see cref="Changed"/> uses
/// the same equality the pipeline uses to decide whether to write.
/// </summary>
public sealed record PlannedValue<T>(T Desired, ServiceValue<T>? Current, bool Changed);

/// <summary>A value read from the service; its own value may be null.</summary>
public sealed record ServiceValue<T>(T Value);

public static class PlannedValue
{
    public static PlannedValue<T> Created<T>(T desired) => new(desired, null, Changed: true);

    public static PlannedValue<T> Compared<T>(T current, T desired) =>
        Compared(current, desired, !EqualityComparer<T>.Default.Equals(current, desired));

    public static PlannedValue<T> Compared<T>(T current, T desired, bool changed) =>
        new(desired, new ServiceValue<T>(current), changed);
}
