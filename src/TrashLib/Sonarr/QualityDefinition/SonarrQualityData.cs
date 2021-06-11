using System.Globalization;
using System.Text;

namespace TrashLib.Sonarr.QualityDefinition
{
    public class SonarrQualityData
    {
        public const decimal MaxUnlimitedThreshold = 400;

        public string Name { get; set; } = "";
        public decimal Min { get; set; }
        public decimal Max { get; set; }

        public decimal? MaxForApi => Max < MaxUnlimitedThreshold ? Max : null;
        public decimal MinForApi => Min;

        public string AnnotatedMin => Min.ToString(CultureInfo.InvariantCulture);
        public string AnnotatedMax => AnnotatedValue(Max, MaxUnlimitedThreshold);

        protected static string AnnotatedValue(decimal value, decimal threshold)
        {
            var builder = new StringBuilder(value.ToString(CultureInfo.InvariantCulture));
            if (value >= threshold)
            {
                builder.Append(" (Unlimited)");
            }

            return builder.ToString();
        }

        public bool IsMinDifferent(decimal serviceValue) => serviceValue != Min;

        public bool IsMaxDifferent(decimal? serviceValue)
        {
            return serviceValue == null
                ? MaxUnlimitedThreshold != Max
                : serviceValue != Max || MaxUnlimitedThreshold == Max;
        }
    }
}
