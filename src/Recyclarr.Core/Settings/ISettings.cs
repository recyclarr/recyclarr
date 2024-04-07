namespace Recyclarr.Settings;

public interface ISettings<out T>
{
    bool IsProvided { get; }
    T Value { get; }
    T? OptionalValue { get; }
}
