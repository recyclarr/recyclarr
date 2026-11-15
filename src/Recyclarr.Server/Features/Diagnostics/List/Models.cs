using Recyclarr.Server.Features.Sync.GetResults;

namespace Recyclarr.Server.Features.Diagnostics.List;

/// <summary>
/// One problem the server has now, such as a deprecated setting or a held-back guide update. It
/// stays in the list while the problem lasts and leaves it once the server sees the problem is
/// fixed. New codes may be added; a client that does not know a code can still show `message`.
/// </summary>
internal sealed record DiagnosticResponse
{
    /// <summary>
    /// Identifies this problem. It stays the same while the problem lasts, so a client can track
    /// it across requests.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>Identifies the kind of problem, for example `environmentVariableRemoved`.</summary>
    public required string Code { get; init; }

    public required OutcomeSeverity Severity { get; init; }

    /// <summary>A description of the problem for people to read.</summary>
    public required string Message { get; init; }

    /// <summary>
    /// Facts specific to the code. Absent when the code has none. The `x-diagnostic-data`
    /// extension of this schema maps each code to the schema of its data.
    /// </summary>
    public object? Data { get; init; }
}

internal sealed record ListDiagnosticsResponse(IReadOnlyList<DiagnosticResponse> Diagnostics);
