using Recyclarr.Sync.Results;

namespace Recyclarr.Pipelines.MediaNaming;

/// <summary>
/// Builds the planned values of settings where only configured fields are managed: a field the
/// configuration leaves null has no planned value.
/// </summary>
internal sealed class ConfiguredValues<TSettings>(
    TSettings current,
    TSettings desired,
    TSettings planned
)
{
    public PlannedValue<TValue>? For<TValue>(Func<TSettings, TValue> select) =>
        select(planned) is null ? null : PlannedValue.Compared(select(current), select(desired));
}

internal static class ConfiguredValues
{
    /// <summary>Pass each value's <c>Changed</c> flag, null for an unconfigured field.</summary>
    public static PlannedAction ActionFor(params bool?[] changed) =>
        changed.Any(x => x == true) ? PlannedAction.Update : PlannedAction.Unchanged;
}
