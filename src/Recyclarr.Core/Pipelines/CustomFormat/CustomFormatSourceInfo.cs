namespace Recyclarr.Pipelines.CustomFormat;

internal record CustomFormatSourceInfo(
    CfSource Source,
    string? GroupName,
    CfInclusionReason InclusionReason,
    IReadOnlyList<string> ProfileNames
);
