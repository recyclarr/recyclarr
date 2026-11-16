namespace Recyclarr.Server.Sync;

internal sealed record ConfigParseFailure(string? FileName, int Line, string Message);

internal sealed record InvalidInstance(string InstanceName, IReadOnlyList<string> Errors);

internal sealed record SplitInstanceGroup(string BaseUrl, IReadOnlyList<string> InstanceNames);

/// <summary>A deprecated property and the configuration file it was found in, when known.</summary>
internal sealed record ConfigDeprecation(string? FileName, string Message);

// Structured record of everything that can go wrong while loading configuration at startup: parse
// failures, filter diagnostics (invalid/duplicate/split instances), and deprecation warnings.
internal sealed record ConfigLoadDiagnostics
{
    public IReadOnlyList<ConfigParseFailure> ParseFailures { get; init; } = [];
    public IReadOnlyList<InvalidInstance> InvalidInstances { get; init; } = [];
    public IReadOnlyList<string> DuplicateInstances { get; init; } = [];
    public IReadOnlyList<SplitInstanceGroup> SplitInstanceGroups { get; init; } = [];
    public IReadOnlyList<ConfigDeprecation> DeprecationWarnings { get; init; } = [];

    public bool HasServerConfigurationErrors =>
        ParseFailures.Count > 0
        || InvalidInstances.Count > 0
        || DuplicateInstances.Count > 0
        || SplitInstanceGroups.Count > 0;

    /// <summary>Each configuration error as one sentence for people to read.</summary>
    public IReadOnlyList<string> DescribeErrors() =>
        [
            .. ParseFailures.Select(x =>
                $"Config parsing failed in {x.FileName ?? "unknown"}: {x.Message}"
            ),
            .. InvalidInstances.SelectMany(x =>
                x.Errors.Select(e => $"Invalid instance {x.InstanceName}: {e}")
            ),
            .. DuplicateInstances.Select(x => $"Duplicate instance: {x}"),
            .. SplitInstanceGroups.Select(x =>
                $"Instances {string.Join(", ", x.InstanceNames)} share the same base URL: "
                + x.BaseUrl
            ),
        ];
}
