namespace Recyclarr.Settings;

internal class Settings<T>(Func<T?> valueFactory) : ISettings<T>
{
    private readonly Lazy<T?> _lazy = new(valueFactory);

    public T? OptionalValue => _lazy.Value;
    public bool IsProvided => OptionalValue is not null;
    public T Value =>
        OptionalValue ?? throw new InvalidOperationException("Settings value is not provided");
}
