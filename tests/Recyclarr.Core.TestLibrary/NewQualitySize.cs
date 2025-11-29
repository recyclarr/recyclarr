using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Core.TestLibrary;

public static class NewQualitySize
{
    public static QualityItem Item(string quality, decimal min, decimal max, decimal preferred)
    {
        return new QualityItem
        {
            Quality = quality,
            Min = min,
            Max = max,
            Preferred = preferred,
        };
    }

    public static QualityItemWithLimits WithLimits(
        string quality,
        decimal min,
        decimal max,
        decimal preferred
    )
    {
        var item = new QualityItem
        {
            Quality = quality,
            Min = min,
            Max = max,
            Preferred = preferred,
        };
        return new QualityItemWithLimits(
            item,
            new QualityItemLimits(
                TestQualityItemLimits.MaxUnlimitedThreshold,
                TestQualityItemLimits.PreferredUnlimitedThreshold
            )
        );
    }
}
