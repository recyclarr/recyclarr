using Autofac;
using DotNext.Threading;
using Recyclarr.Config.Parsing;
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
/// <para>
/// Each refresh also checks the configuration inputs (ADR-019). Changed inputs resolve the
/// configuration again; when that fails, the failure belongs to the edit, so provider updates
/// from the same refresh are reverted without blame and the rejected inputs are not retried until
/// they change again.
/// </para>
/// </remarks>
internal sealed class ResourceCoordinator(
    ILogger log,
    ResourceSourceFactory sourceFactory,
    IResourceCacheCleanupService cleanup,
    ConfigInputs configInputs,
    ILifetimeScope scope,
    TimeProvider time
) : IDisposable
{
    public static readonly TimeSpan Ttl = TimeSpan.FromHours(1);
    public static readonly TimeSpan FetchBackoff = TimeSpan.FromMinutes(5);

    private readonly AsyncReaderWriterLock _lock = new();
    private IReadOnlyList<IResourceSource>? _sources;
    private ResourceSnapshot? _current;

    // Inputs of the published snapshot and of the last resolve that failed, plus the paths that
    // resolve recorded beyond discovery, which the next check hashes too.
    private ConfigInputFingerprint? _acceptedInputs;
    private ConfigInputFingerprint? _rejectedInputs;
    private IReadOnlyCollection<string> _recordedInputs = [];

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

        // Discovery and hashes are taken before resolving, so an edit made during the resolve
        // differs from what is recorded here and is picked up by the next check.
        var discovered = configInputs.Discover();
        var inputs = configInputs.Capture(discovered.Concat(_recordedInputs));
        var inputsSeen = inputs.Equals(_acceptedInputs) || inputs.Equals(_rejectedInputs);
        if (_current is not null && updated.Count == 0 && inputsSeen)
        {
            return;
        }

        var configEdited = _current is not null && !inputs.Equals(_acceptedInputs);
        var resolved = Resolve(sources);
        if (resolved.Snapshot is null && updated.Count > 0)
        {
            var reverted = await RevertAsync(updated, blameProviders: !configEdited, ct);
            if (reverted && !configEdited)
            {
                resolved = Resolve(sources);
            }
        }

        _recordedInputs = resolved.RecordedInputs;
        var resolvedInputs = configInputs.Capture(discovered.Concat(_recordedInputs), inputs);

        if (resolved.Snapshot is not { } snapshot)
        {
            RejectConfiguration(configEdited, resolvedInputs);
            return;
        }

        foreach (var source in sources)
        {
            await source.MarkGoodAsync(ct);
        }

        _current = snapshot;
        _acceptedInputs = resolvedInputs;
        _rejectedInputs = null;
        if (configEdited)
        {
            log.Information("Configuration changed and was reloaded");
        }
    }

    private void RejectConfiguration(bool configEdited, ConfigInputFingerprint inputs)
    {
        if (_current is null)
        {
            throw new FatalException(
                "Server configuration is invalid; correct the errors logged above and restart"
            );
        }

        if (configEdited)
        {
            _rejectedInputs = inputs;
            log.Error(
                "Configuration changed but is invalid; the previous configuration stays in use "
                    + "until the errors logged above are corrected"
            );
            return;
        }

        log.Error(
            "Updated guide data made the configuration invalid and could not be reverted; "
                + "correct the errors logged above"
        );
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

    private async Task<bool> RevertAsync(
        List<IResourceSource> updated,
        bool blameProviders,
        CancellationToken ct
    )
    {
        var reverted = false;
        foreach (var source in updated)
        {
            if (!await source.RevertToGoodAsync(ct))
            {
                continue;
            }

            reverted = true;
            if (blameProviders)
            {
                log.Warning(
                    "Updated data from resource provider {Name} made the configuration invalid; "
                        + "reverted to the last data that validated",
                    source.Config.Name
                );
            }
        }

        return reverted;
    }

    // Configuration is resolved in its own scope bound to the new guide data, so template includes
    // read that data and diagnostics from earlier loads are not repeated. The scope's recorder
    // holds the inputs this resolve read, whether or not it succeeded.
    private ResolvedConfiguration Resolve(IReadOnlyList<IResourceSource> sources)
    {
        var guide = GuideIndex.Build(sources);
        using var resolveScope = scope.BeginLifetimeScope(c =>
            c.RegisterInstance(ResourceScope.For(guide))
        );

        var configuration = resolveScope.Resolve<ServerConfigLoader>().TryLoadServerConfiguration();
        var recorded = resolveScope.Resolve<ConfigInputRecorder>().Paths.ToList();
        var snapshot = configuration is null ? null : new ResourceSnapshot(guide, configuration);
        return new ResolvedConfiguration(snapshot, recorded);
    }

    private sealed record ResolvedConfiguration(
        ResourceSnapshot? Snapshot,
        IReadOnlyCollection<string> RecordedInputs
    );
}
