namespace Recyclarr.Settings;

public interface ISettings<out T>
{
    T Value { get; }
}
