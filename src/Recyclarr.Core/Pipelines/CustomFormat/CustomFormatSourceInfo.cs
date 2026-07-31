namespace Recyclarr.Pipelines.CustomFormat;

public sealed record CustomFormatSourceInfo
{
    public CustomFormatSourceInfo(
        CfSource source,
        string? groupName,
        CfInclusionReason inclusionReason,
        IReadOnlyList<string> profileNames
    )
    {
        Source = source;
        GroupName = groupName;
        InclusionReason = inclusionReason;
        ProfileNames = profileNames.ToList().AsReadOnly();
    }

    public CfSource Source { get; }
    public string? GroupName { get; }
    public CfInclusionReason InclusionReason { get; }
    public IReadOnlyList<string> ProfileNames { get; }
}
