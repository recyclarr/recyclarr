namespace Recyclarr.Pipelines.CustomFormat;

public sealed record CustomFormatSourceInfo(
    CfSource Source,
    string? GroupName,
    CfInclusionReason InclusionReason,
    IReadOnlyList<string> ProfileNames
);
