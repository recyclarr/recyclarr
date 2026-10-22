using Autofac;
using DotNext.Threading;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.ResourceProviders.Storage;
using Recyclarr.Server.Sync;

namespace Recyclarr.Server.Resources;

internal enum RefreshMode
{
    // Before the server reports READY; failures that leave no usable snapshot stop startup.
    Startup,

    // While the server runs; failures keep the current snapshot.
    Background,
}

/// <summary>
/// The single owner of resource provider data and the snapshot derived from it (ADR-031). Only a
/// refresh changes provider files, and only while it holds the write lock; readers hold a read
/// lease for their whole request or sync run, so files never change under them.
/// </summary>
/// <remarks>
/// A refresh never waits for readers: a waiting writer blocks new readers, which would stall guide
/// requests behind a long sync. When readers hold the lock, the refresh is skipped and the next
/// one retries. At startup no readers exist yet, so the lock is always free.
/// </remarks>
internal sealed class ResourceCoordinator(
    ILogger log,
    ResourceSourceFactory sourceFactory,
    IResourceCacheCleanupService cleanup,
    ILifetimeScope scope,
    TimeProvider time
) : IDisposable
{
    public static readonly TimeSpan Ttl = TimeSpan.FromHours(1);
    public static readonly TimeSpan FetchBackoff = TimeSpan.FromMinutes(5);

    private readonly AsyncReaderWriterLock _lock = new();
    private IReadOnlyList<IResourceSource>? _sources;
    private ResourceSnapshot? _current;

    /// <summary>
    /// The published snapshot, for readers of configuration only. Guide data must be read through
    /// <see cref="AcquireReadAsync"/>, which keeps the files from changing.
    /// </summary>
    public ResourceSnapshot Current =>
        _current
        ?? throw new InvalidOperationException("Resources were read before startup loaded them");

    public void Dispose() => _lock.Dispose();

    public async Task<ResourceLease> AcquireReadAsync(CancellationToken ct)
    {
        await _lock.EnterReadLockAsync(ct);
        if (_current is null)
        {
            _lock.Release();
            throw new InvalidOperationException("Resources were read before startup loaded them");
        }

        return new ResourceLease(_lock, _current);
    }

    public async Task RefreshAsync(RefreshMode mode, CancellationToken ct)
    {
        if (!_lock.TryEnterWriteLock())
        {
            log.Debug("Resource refresh skipped: readers hold provider data");
            return;
        }

        try
        {
            await RefreshLockedAsync(mode, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task RefreshLockedAsync(RefreshMode mode, CancellationToken ct)
    {
        var firstRefresh = _sources is null;
        var sources = _sources ??= sourceFactory.CreateAll();

        var updated = new List<IResourceSource>();
        foreach (var source in sources)
        {
            if (await UpdateIfDueAsync(source, mode, ct) == SourceUpdateOutcome.Updated)
            {
                updated.Add(source);
            }
        }

        if (firstRefresh)
        {
            cleanup.CleanupOrphans(sources.SelectMany(s => s.Roots));
        }

        if (_current is not null && updated.Count == 0)
        {
            return;
        }

        var snapshot = Resolve(sources);
        if (snapshot is null && updated.Count > 0)
        {
            snapshot = await RevertAsync(updated, ct) ? Resolve(sources) : null;
        }

        if (snapshot is null)
        {
            if (_current is null)
            {
                throw new FatalException(
                    "Server configuration is invalid; correct the errors logged above and restart"
                );
            }

            log.Error(
                "Updated guide data made the configuration invalid and could not be reverted; "
                    + "correct the errors logged above"
            );
            return;
        }

        foreach (var source in sources)
        {
            await source.MarkGoodAsync(ct);
        }

        _current = snapshot;
    }

    private async Task<SourceUpdateOutcome?> UpdateIfDueAsync(
        IResourceSource source,
        RefreshMode mode,
        CancellationToken ct
    )
    {
        if (source.Tracking is not { } tracking)
        {
            return null;
        }

        var now = time.GetUtcNow();
        var state = ProviderRefreshState.Read(tracking.StateFile);

        // Startup needs data, so a missing cache is fetched even during a backoff.
        var mustFetch = mode == RefreshMode.Startup && source.Roots.Count == 0;
        if (!mustFetch && !IsDue(state, tracking.Identity, now))
        {
            return null;
        }

        log.Information("Updating resource provider {Name}", source.Config.Name);
        try
        {
            var outcome = await source.UpdateAsync(ct);
            var succeeded = outcome != SourceUpdateOutcome.FailedUsingCache;
            if (!succeeded)
            {
                log.Warning(
                    "Resource provider {Name} could not be updated; using its cached data",
                    source.Config.Name
                );
            }

            Record(tracking, state, now, succeeded);
            return outcome;
        }
        catch (Exception e)
            when (mode == RefreshMode.Background && e is not OperationCanceledException)
        {
            Record(tracking, state, now, succeeded: false);
            log.Warning(e, "Resource provider {Name} could not be updated", source.Config.Name);
            return SourceUpdateOutcome.FailedUsingCache;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            Record(tracking, state, now, succeeded: false);
            throw;
        }
    }

    // Stale when never fetched, fetched for a different identity, or older than the TTL. A failed
    // fetch is not retried until the backoff passes, so an outage does not add a network timeout
    // to every refresh.
    private static bool IsDue(ProviderRefreshState? state, string identity, DateTimeOffset now)
    {
        if (state is null || state.SourceIdentity != identity)
        {
            return true;
        }

        if (!state.LastAttemptSucceeded && now - state.LastAttemptAt < FetchBackoff)
        {
            return false;
        }

        return state.LastSuccessAt is not { } success || now - success >= Ttl;
    }

    private static void Record(
        SourceTracking tracking,
        ProviderRefreshState? previous,
        DateTimeOffset now,
        bool succeeded
    )
    {
        var sameIdentity = previous?.SourceIdentity == tracking.Identity;
        new ProviderRefreshState
        {
            LastAttemptAt = now,
            LastAttemptSucceeded = succeeded,
            LastSuccessAt =
                succeeded ? now
                : sameIdentity ? previous?.LastSuccessAt
                : null,
            SourceIdentity = tracking.Identity,
        }.Write(tracking.StateFile);
    }

    private async Task<bool> RevertAsync(List<IResourceSource> updated, CancellationToken ct)
    {
        var reverted = false;
        foreach (var source in updated)
        {
            if (await source.RevertToGoodAsync(ct))
            {
                log.Warning(
                    "Updated data from resource provider {Name} made the configuration invalid; "
                        + "reverted to the last data that validated",
                    source.Config.Name
                );
                reverted = true;
            }
        }

        return reverted;
    }

    // Configuration is resolved in its own scope bound to the new guide data, so template includes
    // read that data and diagnostics from earlier loads are not repeated.
    private ResourceSnapshot? Resolve(IReadOnlyList<IResourceSource> sources)
    {
        var guide = GuideIndex.Build(sources);
        using var resolveScope = scope.BeginLifetimeScope(c =>
            c.RegisterInstance(ResourceScope.For(guide))
        );

        var configuration = resolveScope.Resolve<ServerConfigLoader>().TryLoadServerConfiguration();
        return configuration is null ? null : new ResourceSnapshot(guide, configuration);
    }
}
