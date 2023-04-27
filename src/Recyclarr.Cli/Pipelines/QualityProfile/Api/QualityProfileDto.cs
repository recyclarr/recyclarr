using System.Collections.ObjectModel;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Recyclarr.Cli.Pipelines.QualityProfile.Api;

[UsedImplicitly]
public record QualityProfileDto
{
    public int Id { get; [UsedImplicitly] init; }
    public string Name { get; init; } = "";
    public bool UpgradeAllowed { get; init; }
    public int MinFormatScore { get; init; }
    public int Cutoff { get; init; }
    public int CutoffFormatScore { get; init; }
    public Collection<ProfileFormatItemDto> FormatItems { get; } = new();

    [JsonExtensionData]
    public JObject? ExtraJson { get; init; }
}

[UsedImplicitly]
public record ProfileFormatItemDto
{
    public int Format { get; init; }
    public string Name { get; init; } = "";
    public int Score { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> ExtraJson { get; init; } = new();
}
