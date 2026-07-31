using Recyclarr.Sync.Results;

namespace Recyclarr.Pipelines.MediaNaming;

internal sealed class ConfiguredValueChanges<TSettings>(
    TSettings current,
    TSettings desired,
    TSettings planned
)
{
    public ValueDelta<TValue>? For<TValue>(Func<TSettings, TValue> select)
    {
        var plannedValue = select(planned);
        if (plannedValue is null)
        {
            return null;
        }

        var currentValue = select(current);
        var desiredValue = select(desired);

        return EqualityComparer<TValue>.Default.Equals(currentValue, desiredValue)
            ? null
            : new ValueDelta<TValue>(currentValue, desiredValue);
    }
}
