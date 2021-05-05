using System;
using System.Globalization;
using System.Text;

namespace Trash.Radarr.QualityDefinition
{
    public class RadarrQualityData
    {
        public const decimal Tolerance = 0.1m;
        public const decimal MaxUnlimitedThreshold = 400;
        public const decimal PreferredUnlimitedThreshold = 395;

        public string Name { get; set; } = "";
        public decimal Min { get; set; }
        public decimal Max { get; set; }
        public decimal Preferred { get; set; }

        public decimal? MaxForApi => Max < MaxUnlimitedThreshold ? Max : null;
        public decimal MinForApi => Min;
        public decimal? PreferredForApi => Preferred < PreferredUnlimitedThreshold ? Preferred : null;

        public string AnnotatedMin => Min.ToString(CultureInfo.InvariantCulture);
        public string AnnotatedMax => AnnotatedValue(Max, MaxUnlimitedThreshold);
        public string AnnotatedPreferred => AnnotatedValue(Preferred, PreferredUnlimitedThreshold);

        public decimal InterpolatedPreferred(decimal ratio)
        {
            var cappedMax = Math.Min(Max, PreferredUnlimitedThreshold);
            return Math.Round(Min + (cappedMax - Min) * ratio, 1);
        }

        private static string AnnotatedValue(decimal value, decimal threshold)
        {
            var builder = new StringBuilder(value.ToString(CultureInfo.InvariantCulture));
            if (value >= threshold)
            {
                builder.Append(" (Unlimited)");
            }

            return builder.ToString();
        }

        public bool MinOutsideTolerance(decimal other) =>
            Math.Abs(other - Min) > Tolerance;

        public bool MaxOutsideTolerance(decimal? other) =>
            Math.Abs((other ?? MaxUnlimitedThreshold) - Max) > Tolerance;

        public bool PreferredOutsideTolerance(decimal? other) =>
            Math.Abs((other ?? PreferredUnlimitedThreshold) - Preferred) > Tolerance;
    }
}
