using TUnit.Core.Interfaces;
using SizeSnapshot = (double? Min, double? Max, double? Preferred);
using SonarrApi = Recyclarr.Api.Sonarr.V3;

namespace Recyclarr.EndToEndTests;

/// <summary>
/// Services and a running server shared by every ordered test of one test class. Later tests
/// build on the service state earlier tests leave behind, so one session lives from before the
/// first test of its class until after the last.
/// </summary>
internal abstract class SyncSession(string sonarrImageTag, bool startRadarr)
    : IAsyncInitializer,
        IAsyncDisposable
{
    public const string MainConfig = "recyclarr.yml";

    private RecyclarrTestHarness? _harness;

    public RecyclarrTestHarness Harness =>
        _harness ?? throw new InvalidOperationException("The session has not started.");

    public bool SonarrV5 { get; private set; }

    /// <summary>
    /// HDTV-1080p sizes of every Sonarr v5 profile before the first sync, so a later check can
    /// prove that profiles the config does not manage keep their sizes.
    /// </summary>
    public IReadOnlyDictionary<string, SizeSnapshot> UnmanagedProfileSizesBeforeSync { get; set; } =
        new Dictionary<string, SizeSnapshot>();

    public async Task InitializeAsync()
    {
        // TUnit gives shared data source initialization no timeout or token, so the session bounds
        // its own startup. CI startup, image pulls included, normally takes under 40 seconds and
        // took 76 seconds at worst; the limit leaves room for that while catching a hang early.
        var limit = TimeSpan.FromMinutes(3);
        using var timeout = new CancellationTokenSource(limit);
        try
        {
            await StartAsync(timeout.Token);
        }
        catch (OperationCanceledException e) when (timeout.IsCancellationRequested)
        {
            throw new TimeoutException($"E2E session startup exceeded {limit}", e);
        }
    }

    private async Task StartAsync(CancellationToken ct)
    {
        _harness = await RecyclarrTestHarness.StartAsync(sonarrImageTag, startRadarr, ct);
        await _harness.StartServerAsync(MainConfig, ct);

        var status = await _harness.SonarrApi<SonarrApi.ISystemApi>().Status(ct);
        SonarrV5 = new Version(status.Version!).Major >= 5;
    }

    public async ValueTask DisposeAsync()
    {
        if (_harness is not null)
        {
            await _harness.DisposeAsync();
        }
    }
}

/// <summary>Sonarr <c>latest</c> is v4 (API v3). Radarr runs in this session only.</summary>
internal sealed class SonarrV4WithRadarrSession() : SyncSession("latest", startRadarr: true);

/// <summary>Sonarr <c>develop</c> is v5 (API v5).</summary>
internal sealed class SonarrV5Session() : SyncSession("develop", startRadarr: false);
