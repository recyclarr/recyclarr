using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Recyclarr.Cli.Pipelines.QualityProfile.Api;

public static class DtoUtil
{
    public static void SetIfNotNull<T>(ref T propertyValue, T? newValue)
    {
        if (newValue is not null)
        {
            propertyValue = newValue;
        }
    }
}

[UsedImplicitly]
public record QualityProfileDto
{
    private readonly bool? _upgradeAllowed;
    private readonly int? _minFormatScore;
    private readonly int? _cutoff;
    private readonly int? _cutoffFormatScore;
    private readonly string _name = "";
    private readonly IReadOnlyCollection<ProfileItemDto> _items = new List<ProfileItemDto>();

    public int? Id { get; set; }

    public string Name
    {
        get => _name;
        init
        {
            if (string.IsNullOrEmpty(_name))
            {
                _name = value;
            }
        }
    }

    public bool? UpgradeAllowed
    {
        get => _upgradeAllowed;
        init => DtoUtil.SetIfNotNull(ref _upgradeAllowed, value);
    }

    public int? MinFormatScore
    {
        get => _minFormatScore;
        init => DtoUtil.SetIfNotNull(ref _minFormatScore, value);
    }

    public int? Cutoff
    {
        get => _cutoff;
        init => DtoUtil.SetIfNotNull(ref _cutoff, value);
    }

    public int? CutoffFormatScore
    {
        get => _cutoffFormatScore;
        init => DtoUtil.SetIfNotNull(ref _cutoffFormatScore, value);
    }

    public IReadOnlyCollection<ProfileFormatItemDto> FormatItems { get; init; } = new List<ProfileFormatItemDto>();

    public IReadOnlyCollection<ProfileItemDto> Items
    {
        get => _items;
        init
        {
            if (value.Count > 0)
            {
                _items = value;
            }
        }
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
    private readonly bool? _allowed;

    public int? Id { get; set; }
    public string? Name { get; init; }

    public bool? Allowed
    {
        get => _allowed;
        init => DtoUtil.SetIfNotNull(ref _allowed, value);
    }

    public ProfileItemQualityDto? Quality { get; init; }
    public ICollection<ProfileItemDto> Items { get; init; } = new List<ProfileItemDto>();

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
