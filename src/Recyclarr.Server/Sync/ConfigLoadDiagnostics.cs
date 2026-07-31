namespace Recyclarr.Server.Sync;

internal sealed record ConfigParseFailure(string? FileName, int Line, string Message);

internal sealed record InvalidInstance(string InstanceName, IReadOnlyList<string> Errors);

internal sealed record SplitInstanceGroup(string BaseUrl, IReadOnlyList<string> InstanceNames);

// Structured, renderer-agnostic record of everything that can go wrong while loading configs for
// a sync request: parse failures, filter diagnostics (unknown/invalid/duplicate/split instances),
// and deprecation warnings. Endpoints project this into wire DTOs; no prose is composed here.
internal sealed record ConfigLoadDiagnostics
{
    public IReadOnlyList<string> MissingConfigFiles { get; init; } = [];
    public IReadOnlyList<ConfigParseFailure> ParseFailures { get; init; } = [];
    public IReadOnlyList<string> UnknownInstances { get; init; } = [];
    public IReadOnlyList<string> AvailableInstances { get; init; } = [];
    public IReadOnlyList<InvalidInstance> InvalidInstances { get; init; } = [];
    public IReadOnlyList<string> DuplicateInstances { get; init; } = [];
    public IReadOnlyList<SplitInstanceGroup> SplitInstanceGroups { get; init; } = [];
    public IReadOnlyList<string> DeprecationWarnings { get; init; } = [];

    public bool IsEmpty =>
        MissingConfigFiles.Count == 0
        && ParseFailures.Count == 0
        && UnknownInstances.Count == 0
        && InvalidInstances.Count == 0
        && DuplicateInstances.Count == 0
        && SplitInstanceGroups.Count == 0
        && DeprecationWarnings.Count == 0;
}
