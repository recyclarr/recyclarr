using System.Globalization;
using System.Text;

namespace Recyclarr.TrashGuide.QualitySize;

public class QualityItemWithLimits(QualityItem item, IQualityItemLimits limits)
{
    public QualityItem Item => item;
    public IQualityItemLimits Limits => limits;

    public decimal MinForApi => item.Min;
    public decimal? PreferredForApi => item.Preferred < limits.PreferredLimit ? item.Preferred : null;
    public decimal? MaxForApi => item.Max < limits.MaxLimit ? item.Max : null;

    public string AnnotatedMin => item.Min.ToString(CultureInfo.InvariantCulture);
    public string AnnotatedPreferred => AnnotatedValue(item.Preferred, limits.PreferredLimit);
    public string AnnotatedMax => AnnotatedValue(item.Max, limits.MaxLimit);

    private static string AnnotatedValue(decimal value, decimal threshold)
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
        return serviceValue != item.Min;
    }

    public bool IsPreferredDifferent(decimal? serviceValue)
    {
        return ValueWithThresholdIsDifferent(serviceValue, item.Preferred, limits.PreferredLimit);
    }

    public bool IsMaxDifferent(decimal? serviceValue)
    {
        return ValueWithThresholdIsDifferent(serviceValue, item.Max, limits.MaxLimit);
    }

    private static bool ValueWithThresholdIsDifferent(decimal? serviceValue, decimal guideValue, decimal threshold)
    {
        return serviceValue == null
            // If the service uses null, it's the same if the guide value == the max that null represents
            ? guideValue != threshold
            // If the service value is not null, it's the same only if it isn't the max value or the same as the guide
            // If it's at max, that means we need to switch it to 'null' on the API so it gets treated as unlimited.
            : guideValue != serviceValue || guideValue == threshold;
    }

    public decimal InterpolatedPreferred(decimal ratio)
    {
        var cappedMax = Math.Min(item.Max, limits.PreferredLimit);
        return Math.Round(item.Min + (cappedMax - item.Min) * ratio, 1);
    }
}
