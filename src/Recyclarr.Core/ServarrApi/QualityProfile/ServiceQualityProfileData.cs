using System.Text.Json.Serialization;
using Recyclarr.SyncState;

namespace Recyclarr.ServarrApi.QualityProfile;

[UsedImplicitly]
public record ServiceQualityProfileData : IServiceResource
{
    public int? Id { get; set; }

    // Explicit interface implementation - only valid for profiles fetched from service (which always have Id)
    int IServiceResource.Id =>
        Id ?? throw new InvalidOperationException("ServiceQualityProfileData.Id is null");

    public string Name { get; init; } = "";

    public bool? UpgradeAllowed
    {
        get;
        init => DtoUtil.SetIfNotNull(ref field, value);
    }

    public int? MinFormatScore
    {
        get;
        init => DtoUtil.SetIfNotNull(ref field, value);
    }

    public int? MinUpgradeFormatScore
    {
        get;
        init => DtoUtil.SetIfNotNull(ref field, value);
    }

    public int? Cutoff
    {
        get;
        set => DtoUtil.SetIfNotNull(ref field, value);
    }

    public int? CutoffFormatScore
    {
        get;
        init => DtoUtil.SetIfNotNull(ref field, value);
    }

    public IReadOnlyCollection<ServiceProfileFormatItem> FormatItems { get; init; } = [];

    public IReadOnlyCollection<ServiceProfileItem> Items
    {
        get;
        set
        {
            if (value.Count > 0)
            {
                field = value;
            }
        }
    } = [];

    public ServiceProfileLanguage? Language
    {
        get;
        init => DtoUtil.SetIfNotNull(ref field, value);
    }

    [UsedImplicitly, JsonExtensionData]
    public Dictionary<string, object> ExtraJson { get; init; } = [];
}

[UsedImplicitly]
public record ServiceProfileFormatItem
{
    public int Format { get; init; }
    public string Name { get; init; } = "";
    public int Score { get; init; }

    [UsedImplicitly, JsonExtensionData]
    public Dictionary<string, object> ExtraJson { get; init; } = [];
}

[UsedImplicitly]
public record ServiceProfileItem
{
    public int? Id { get; set; }
    public string? Name { get; init; }

    public bool? Allowed
    {
        get;
        init => DtoUtil.SetIfNotNull(ref field, value);
    }

    public ServiceProfileItemQuality? Quality { get; init; }
    public ICollection<ServiceProfileItem> Items { get; init; } = [];

    [UsedImplicitly, JsonExtensionData]
    public Dictionary<string, object> ExtraJson { get; init; } = [];
}

[UsedImplicitly]
public record ServiceProfileItemQuality
{
    public int? Id { get; init; }
    public string? Name { get; init; }

    [UsedImplicitly, JsonExtensionData]
    public Dictionary<string, object> ExtraJson { get; init; } = [];
}

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record ServiceProfileLanguage
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
}
