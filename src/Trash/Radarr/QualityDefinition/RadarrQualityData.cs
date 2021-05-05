using System;
using Trash.Sonarr.QualityDefinition;

namespace Trash.Radarr.QualityDefinition
{
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

        public bool PreferredOutsideTolerance(decimal? other) =>
            Math.Abs((other ?? PreferredUnlimitedThreshold) - Preferred) > Tolerance;
    }
}
