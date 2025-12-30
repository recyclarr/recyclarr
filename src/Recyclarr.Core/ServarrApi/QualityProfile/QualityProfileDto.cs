using System.Text.Json.Serialization;
using Recyclarr.Cache;

namespace Recyclarr.ServarrApi.QualityProfile;

[UsedImplicitly]
public record QualityProfileDto : IServiceResource
{
    public int? Id { get; set; }

    // Explicit interface implementation - only valid for profiles fetched from service (which always have Id)
    int IServiceResource.Id =>
        Id ?? throw new InvalidOperationException("QualityProfileDto.Id is null");

    public string Name
    {
        get;
        init
        {
            if (string.IsNullOrEmpty(field))
            {
                field = value;
            }
        }
    } = "";

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

    public IReadOnlyCollection<ProfileFormatItemDto> FormatItems { get; init; } = [];

    public IReadOnlyCollection<ProfileItemDto> Items
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

    public ProfileLanguageDto? Language
    {
        get;
        init => DtoUtil.SetIfNotNull(ref field, value);
    }

    [UsedImplicitly, JsonExtensionData]
    public Dictionary<string, object> ExtraJson { get; init; } = new();
}

[UsedImplicitly]
public record ProfileFormatItemDto
{
    public int Format { get; init; }
    public string Name { get; init; } = "";
    public int Score { get; init; }

    [UsedImplicitly, JsonExtensionData]
    public Dictionary<string, object> ExtraJson { get; init; } = new();
}

[UsedImplicitly]
public record ProfileItemDto
{
    public int? Id { get; set; }
    public string? Name { get; init; }

    public bool? Allowed
    {
        get;
        init => DtoUtil.SetIfNotNull(ref field, value);
    }

    public ProfileItemQualityDto? Quality { get; init; }
    public ICollection<ProfileItemDto> Items { get; init; } = [];

    [UsedImplicitly, JsonExtensionData]
    public Dictionary<string, object> ExtraJson { get; init; } = new();
}

[UsedImplicitly]
public record ProfileItemQualityDto
{
    public int? Id { get; init; }
    public string? Name { get; init; }

    [UsedImplicitly, JsonExtensionData]
    public Dictionary<string, object> ExtraJson { get; init; } = new();
}

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record ProfileLanguageDto
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
}
