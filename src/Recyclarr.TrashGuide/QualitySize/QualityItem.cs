using System.Globalization;
using System.Text;

namespace Recyclarr.TrashGuide.QualitySize;

public class QualityItem(string quality, decimal min, decimal max)
{
    public const decimal MaxUnlimitedThreshold = 400;

    public string Quality { get; } = quality;
    public decimal Min { get; } = min;
    public decimal Max { get; } = max;

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

    public bool IsMinDifferent(decimal serviceValue)
    {
        return serviceValue != Min;
    }

    protected static bool ValueWithThresholdIsDifferent(decimal? serviceValue, decimal guideValue, decimal threshold)
    {
        return serviceValue == null
            // If the service uses null, it's the same if the guide value == the max that null represents
            ? guideValue != threshold
            // If the service value is not null, it's the same only if it isn't the max value or the same as the guide
            // If it's at max, that means we need to switch it to 'null' on the API so it gets treated as unlimited.
            : guideValue != serviceValue || guideValue == threshold;
    }

    public bool IsMaxDifferent(decimal? serviceValue)
    {
        return ValueWithThresholdIsDifferent(serviceValue, Max, MaxUnlimitedThreshold);
    }
}
