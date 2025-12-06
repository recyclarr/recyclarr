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
}
