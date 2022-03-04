using TrashLib.Sonarr.QualityDefinition;

namespace TrashLib.Radarr.QualityDefinition;

public class RadarrQualityData : SonarrQualityData
{
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
