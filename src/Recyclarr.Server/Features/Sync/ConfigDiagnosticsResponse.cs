using Recyclarr.Server.Sync;

namespace Recyclarr.Server.Features.Sync;

// Shared wire DTO for structured config-load diagnostics, used by both CreateJob's 400 response
// (nothing left to sync) and GetJob's job resource (job proceeded with a subset of instances).

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record ConfigParseFailureResponse(string? FileName, int Line, string Message);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record InvalidInstanceResponse(string InstanceName, IReadOnlyList<string> Errors);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record SplitInstanceGroupResponse(
    string BaseUrl,
    IReadOnlyList<string> InstanceNames
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed record ConfigDiagnosticsResponse
{
    public IReadOnlyList<string> MissingConfigFiles { get; init; } = [];
    public IReadOnlyList<ConfigParseFailureResponse> ParseFailures { get; init; } = [];
    public IReadOnlyList<string> UnknownInstances { get; init; } = [];
    public IReadOnlyList<string> AvailableInstances { get; init; } = [];
    public IReadOnlyList<InvalidInstanceResponse> InvalidInstances { get; init; } = [];
    public IReadOnlyList<string> DuplicateInstances { get; init; } = [];
    public IReadOnlyList<SplitInstanceGroupResponse> SplitInstanceGroups { get; init; } = [];
    public IReadOnlyList<string> DeprecationWarnings { get; init; } = [];
}

internal static class ConfigDiagnosticsResponseMapper
{
    public static ConfigDiagnosticsResponse ToResponse(this ConfigLoadDiagnostics diagnostics)
    {
        return new ConfigDiagnosticsResponse
        {
            MissingConfigFiles = diagnostics.MissingConfigFiles,
            ParseFailures = diagnostics
                .ParseFailures.Select(f => new ConfigParseFailureResponse(
                    f.FileName,
                    f.Line,
                    f.Message
                ))
                .ToList(),
            UnknownInstances = diagnostics.UnknownInstances,
            AvailableInstances = diagnostics.AvailableInstances,
            InvalidInstances = diagnostics
                .InvalidInstances.Select(i => new InvalidInstanceResponse(i.InstanceName, i.Errors))
                .ToList(),
            DuplicateInstances = diagnostics.DuplicateInstances,
            SplitInstanceGroups = diagnostics
                .SplitInstanceGroups.Select(s => new SplitInstanceGroupResponse(
                    s.BaseUrl,
                    s.InstanceNames
                ))
                .ToList(),
            DeprecationWarnings = diagnostics.DeprecationWarnings,
        };
    }
}
