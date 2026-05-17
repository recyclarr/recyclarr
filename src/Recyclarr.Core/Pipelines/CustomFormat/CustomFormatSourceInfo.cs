namespace Recyclarr.Pipelines.CustomFormat;

public record CustomFormatSourceInfo(
    CfSource Source,
    string? GroupName,
    CfInclusionReason InclusionReason,
    IReadOnlyList<string> ProfileNames
);
