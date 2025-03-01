using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Core.TestLibrary;

public static class NewQualitySize
{
    public static QualityItemWithLimits WithLimits(
        string quality,
        decimal min,
        decimal max,
        decimal preferred
    )
    {
        var item = new QualityItem(quality, min, max, preferred);
        return new QualityItemWithLimits(
            item,
            new QualityItemLimits(
                TestQualityItemLimits.MaxUnlimitedThreshold,
                TestQualityItemLimits.PreferredUnlimitedThreshold
            )
        );
    }
}
