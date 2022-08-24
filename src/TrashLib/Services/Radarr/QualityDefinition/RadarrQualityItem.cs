using TrashLib.Services.Common.QualityDefinition;

namespace TrashLib.Services.Radarr.QualityDefinition;

public class RadarrQualityItem : QualityItem
{
    public RadarrQualityItem(string quality, decimal min, decimal max, decimal preferred)
        : base(quality, min, max)
    {
        Preferred = preferred;
    }

    public const decimal PreferredUnlimitedThreshold = 395;

    public decimal Preferred { get; set; }
    public decimal? PreferredForApi => Preferred < PreferredUnlimitedThreshold ? Preferred : null;
    public string AnnotatedPreferred => AnnotatedValue(Preferred, PreferredUnlimitedThreshold);

    public decimal InterpolatedPreferred(decimal ratio)
    {
        var cappedMax = Math.Min(Max, PreferredUnlimitedThreshold);
        return Math.Round(Min + (cappedMax - Min) * ratio, 1);
    }

    public bool IsPreferredDifferent(decimal? serviceValue)
    {
        return serviceValue == null
            ? PreferredUnlimitedThreshold != Preferred
            : serviceValue != Preferred || PreferredUnlimitedThreshold == Preferred;
    }
}
