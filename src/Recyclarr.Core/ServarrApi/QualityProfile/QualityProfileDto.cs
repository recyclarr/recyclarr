using System.Text.Json.Serialization;

namespace Recyclarr.ServarrApi.QualityProfile;

[UsedImplicitly]
public record QualityProfileDto
{
    private readonly bool? _upgradeAllowed;
    private readonly int? _minFormatScore;
    private readonly int? _minFormatUpgradeScore;
    private int? _cutoff;
    private readonly int? _cutoffFormatScore;
    private readonly string _name = "";
    private IReadOnlyCollection<ProfileItemDto> _items = [];

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

    public int? MinFormatUpgradeScore
    {
        get => _minFormatUpgradeScore;
        init => DtoUtil.SetIfNotNull(ref _minFormatUpgradeScore, value);
    }

    public int? Cutoff
    {
        get => _cutoff;
        set => DtoUtil.SetIfNotNull(ref _cutoff, value);
    }

    public int? CutoffFormatScore
    {
        get => _cutoffFormatScore;
        init => DtoUtil.SetIfNotNull(ref _cutoffFormatScore, value);
    }

    public IReadOnlyCollection<ProfileFormatItemDto> FormatItems { get; init; } = [];

    public IReadOnlyCollection<ProfileItemDto> Items
    {
        get => _items;
        set
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
