namespace Recyclarr.Sync;

public readonly record struct JobId
{
    public Guid Value { get; init; }

    public static JobId New() => new() { Value = Guid.NewGuid() };
}
