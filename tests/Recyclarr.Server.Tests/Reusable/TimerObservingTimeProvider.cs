using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Time.Testing;

namespace Recyclarr.Server.Tests.Reusable;

/// <summary>
/// Fake time that signals each timer it creates. Background services start their loops on a
/// thread-pool thread, so a test that advances the clock before a loop creates its timer would
/// skip the tick. Awaiting <see cref="NextTimer"/> first makes advancing the clock deterministic.
/// </summary>
/// <remarks>
/// Timers are told apart by period: <c>Task.Delay</c> creates one-shot timers
/// (<see cref="Timeout.InfiniteTimeSpan"/>); a <c>PeriodicTimer</c> uses its interval.
/// </remarks>
internal sealed class TimerObservingTimeProvider : FakeTimeProvider
{
    private readonly ConcurrentDictionary<TimeSpan, Channel<bool>> _created = [];

    public TimerObservingTimeProvider() { }

    public TimerObservingTimeProvider(DateTimeOffset start)
        : base(start) { }

    /// <summary>
    /// Completes when the next timer with <paramref name="period"/> is created, or immediately for
    /// one created earlier and not yet awaited.
    /// </summary>
    public async Task NextTimer(TimeSpan period)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await Created(period).Reader.ReadAsync(timeout.Token);
    }

    public override ITimer CreateTimer(
        TimerCallback callback,
        object? state,
        TimeSpan dueTime,
        TimeSpan period
    )
    {
        var timer = base.CreateTimer(callback, state, dueTime, period);
        Created(period).Writer.TryWrite(true);
        return timer;
    }

    private Channel<bool> Created(TimeSpan period) =>
        _created.GetOrAdd(period, _ => Channel.CreateUnbounded<bool>());
}
