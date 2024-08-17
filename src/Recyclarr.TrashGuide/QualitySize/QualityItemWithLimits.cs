using System.Globalization;
using System.Text;

namespace Recyclarr.TrashGuide.QualitySize;

public class QualityItemWithLimits(QualityItem item, QualityItemLimits limits)
{
    public QualityItem Item { get; } = item with
    {
        Max = Math.Min(item.Max, limits.MaxLimit),
        Preferred = Math.Min(item.Preferred, limits.PreferredLimit)
    };

    public QualityItemLimits Limits => limits;

    public decimal MinForApi => Item.Min;
    public decimal? PreferredForApi => Item.Preferred < Limits.PreferredLimit ? Item.Preferred : null;
    public decimal? MaxForApi => Item.Max < Limits.MaxLimit ? Item.Max : null;

    public string AnnotatedMin => Item.Min.ToString(CultureInfo.InvariantCulture);
    public string AnnotatedPreferred => AnnotatedValue(Item.Preferred, Limits.PreferredLimit);
    public string AnnotatedMax => AnnotatedValue(Item.Max, Limits.MaxLimit);

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
        return serviceValue != Item.Min;
    }

    public bool IsPreferredDifferent(decimal? serviceValue)
    {
        return ValueWithThresholdIsDifferent(serviceValue, Item.Preferred, Limits.PreferredLimit);
    }

    public bool IsMaxDifferent(decimal? serviceValue)
    {
        return ValueWithThresholdIsDifferent(serviceValue, Item.Max, Limits.MaxLimit);
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
        var cappedMax = Math.Min(Item.Max, Limits.PreferredLimit);
        return Math.Round(Item.Min + (cappedMax - Item.Min) * ratio, 1);
    }
}
