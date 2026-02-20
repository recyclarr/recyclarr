using System.IO.Abstractions;
using Recyclarr.TrashGuide;

namespace Recyclarr.Config.Models;

public abstract record ServiceConfiguration : IServiceConfiguration
{
    public abstract SupportedServices ServiceType { get; }
    public required string InstanceName { get; init; }
    public IFileInfo? YamlPath { get; init; }

    public Uri BaseUrl { get; set; } = new("about:empty");
    public string ApiKey { get; init; } = "";

    public ICollection<CustomFormatConfig> CustomFormats { get; init; } = [];
    public CustomFormatGroupsConfig CustomFormatGroups { get; init; } = new();

    public bool DeleteOldCustomFormats { get; [UsedImplicitly] init; }

    public MediaManagementConfig MediaManagement { get; init; } = new();

    public QualityDefinitionConfig? QualityDefinition { get; init; }

    public IReadOnlyCollection<QualityProfileConfig> QualityProfiles { get; init; } = [];
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record CustomFormatConfig
{
    public ICollection<string> TrashIds { get; init; } = [];

    public ICollection<AssignScoresToConfig> AssignScoresTo { get; init; } = [];
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record AssignScoresToConfig
{
    public string? TrashId { get; init; }
    public string Name { get; init; } = "";
    public int? Score { get; init; }
}

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record CfGroupAssignScoresToConfig
{
    public string? TrashId { get; init; }
    public string? Name { get; init; }
}

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record CustomFormatGroupConfig
{
    public string TrashId { get; init; } = "";
    public ICollection<CfGroupAssignScoresToConfig> AssignScoresTo { get; init; } = [];
    public ICollection<string> Select { get; init; } = [];
    public ICollection<string> Exclude { get; init; } = [];
}

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record CustomFormatGroupsConfig
{
    public ICollection<string> Skip { get; init; } = [];
    public ICollection<CustomFormatGroupConfig> Add { get; init; } = [];
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record QualityDefinitionItemConfig
{
    public string Name { get; init; } = "";
    public QualitySizeValue? Min { get; init; }
    public QualitySizeValue? Max { get; init; }
    public QualitySizeValue? Preferred { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record QualityDefinitionConfig
{
    public string Type { get; init; } = "";
    public decimal? PreferredRatio { get; set; }
    public IReadOnlyCollection<QualityDefinitionItemConfig> Qualities { get; init; } = [];
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record QualityProfileQualityConfig
{
    public string Name { get; init; } = "";
    public bool Enabled { get; init; }
    public IReadOnlyCollection<string> Qualities { get; init; } = [];
}

public enum QualitySortAlgorithm
{
    Top,
    Bottom,
}

public record ResetUnmatchedScoresConfig
{
    public bool Enabled { get; init; }
    public IReadOnlyCollection<string> Except { get; init; } = [];
}

public record QualityProfileConfig
{
    public string? TrashId { get; init; }
    public string Name { get; init; } = "";
    public bool? UpgradeAllowed { get; init; }
    public string? UpgradeUntilQuality { get; init; }
    public int? UpgradeUntilScore { get; init; }
    public int? MinFormatScore { get; init; }
    public int? MinUpgradeFormatScore { get; init; }
    public string? ScoreSet { get; init; }
    public ResetUnmatchedScoresConfig ResetUnmatchedScores { get; init; } = new();
    public QualitySortAlgorithm QualitySort { get; init; }
    public IReadOnlyCollection<QualityProfileQualityConfig> Qualities { get; init; } = [];
}
