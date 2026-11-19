using System.Collections.Immutable;
using Recyclarr.Server.Features.Diagnostics.List;

namespace Recyclarr.Server.Diagnostics;

/// <summary>
/// The problems the server has now (ADR-035). This is current state, not a history: reading it
/// changes nothing, and an entry exists only while its producer last reported it. Each producer
/// owns one source and replaces that source's whole set every time it re-checks its condition, so
/// a producer never adds to or removes another producer's entries. The set lives in memory only;
/// after a restart, each producer rebuilds its entries the next time it checks.
/// </summary>
internal sealed class ServerDiagnostics
{
    private ImmutableDictionary<string, ImmutableArray<DiagnosticResponse>> _sources = [];

    /// <summary>Every current diagnostic: errors first, then by ID.</summary>
    public IReadOnlyList<DiagnosticResponse> Current =>
        _sources
            .Values.SelectMany(x => x)
            .DistinctBy(x => x.Id)
            .OrderBy(x => x.Severity)
            .ThenBy(x => x.Id, StringComparer.Ordinal)
            .ToList();

    public void Replace(string source, IEnumerable<DiagnosticResponse> diagnostics)
    {
        var entries = diagnostics.ToImmutableArray();
        ImmutableInterlocked.Update(ref _sources, x =>
            entries.IsEmpty ? x.Remove(source) : x.SetItem(source, entries)
        );
    }

    public void Clear(string source) => Replace(source, []);
}
