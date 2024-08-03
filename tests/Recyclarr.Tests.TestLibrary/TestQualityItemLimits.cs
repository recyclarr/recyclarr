using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Tests.TestLibrary;

public class TestQualityItemLimits : IQualityItemLimits
{
    public const decimal MaxUnlimitedThreshold = 400m;
    public const decimal PreferredUnlimitedThreshold = 400m;

    public decimal MaxLimit { get; set; } = MaxUnlimitedThreshold;
    public decimal PreferredLimit { get; set; } = PreferredUnlimitedThreshold;
}
