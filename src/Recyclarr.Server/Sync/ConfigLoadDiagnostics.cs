namespace Recyclarr.Server.Sync;

internal sealed record ConfigParseFailure(string? FileName, int Line, string Message);

internal sealed record InvalidInstance(string InstanceName, IReadOnlyList<string> Errors);

internal sealed record SplitInstanceGroup(string BaseUrl, IReadOnlyList<string> InstanceNames);

// Structured record of everything that can go wrong while loading configuration at startup: parse
// failures, filter diagnostics (invalid/duplicate/split instances), and deprecation warnings.
internal sealed record ConfigLoadDiagnostics
{
    public IReadOnlyList<ConfigParseFailure> ParseFailures { get; init; } = [];
    public IReadOnlyList<InvalidInstance> InvalidInstances { get; init; } = [];
    public IReadOnlyList<string> DuplicateInstances { get; init; } = [];
    public IReadOnlyList<SplitInstanceGroup> SplitInstanceGroups { get; init; } = [];
    public IReadOnlyList<string> DeprecationWarnings { get; init; } = [];

    public bool HasServerConfigurationErrors =>
        ParseFailures.Count > 0
        || InvalidInstances.Count > 0
        || DuplicateInstances.Count > 0
        || SplitInstanceGroups.Count > 0;
}
