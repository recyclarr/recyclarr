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

    public bool IsMaxDifferent(decimal? serviceValue)
    {
        return serviceValue == null
            ? MaxUnlimitedThreshold != Max
            : serviceValue != Max || MaxUnlimitedThreshold == Max;
    }
}
