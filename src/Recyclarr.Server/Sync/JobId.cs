namespace Recyclarr.Server.Sync;

/// <summary>
/// Identifies a sync job: the durable, client-visible handle on one sync run. Runs themselves have
/// no identity in the engine; a run is bounded by its lifetime scope.
/// </summary>
internal readonly record struct JobId
{
    public Guid Value { get; init; }

    public static JobId New() => new() { Value = Guid.NewGuid() };
}
