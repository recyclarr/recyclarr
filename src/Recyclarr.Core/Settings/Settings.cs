namespace Recyclarr.Settings;

internal record Settings<T>(T? OptionalValue) : ISettings<T>
{
    public bool IsProvided => OptionalValue is not null;
    public T Value =>
        OptionalValue ?? throw new InvalidOperationException("Settings value is not provided");
}
