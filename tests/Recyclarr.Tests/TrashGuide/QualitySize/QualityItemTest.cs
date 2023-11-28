using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Tests.TrashGuide.QualitySize;

[TestFixture]
public class QualityItemTest
{
    private static readonly object[] MaxTestValues =
    {
        new object?[] {100m, 100m, false},
        new object?[] {100m, 101m, true},
        new object?[] {100m, 98m, true},
        new object?[] {100m, null, true},
        new object?[] {QualityItem.MaxUnlimitedThreshold, null, false},
        new object?[] {QualityItem.MaxUnlimitedThreshold - 1, null, true},
        new object?[] {QualityItem.MaxUnlimitedThreshold, QualityItem.MaxUnlimitedThreshold, true}
    };

    private static readonly object[] MinTestValues =
    {
        new object?[] {0m, 0m, false},
        new object?[] {0m, -1m, true},
        new object?[] {0m, 1m, true}
    };

    [TestCaseSource(nameof(MaxTestValues))]
    public void MaxDifferent_WithVariousValues_ReturnsExpectedResult(
        decimal guideValue,
        decimal? radarrValue,
        bool isDifferent)
    {
        var data = new QualityItem("", 0, guideValue);
        data.IsMaxDifferent(radarrValue)
            .Should().Be(isDifferent);
    }

    [TestCaseSource(nameof(MinTestValues))]
    public void MinDifferent_WithVariousValues_ReturnsExpectedResult(
        decimal guideValue,
        decimal radarrValue,
        bool isDifferent)
    {
        var data = new QualityItem("", guideValue, 0);
        data.IsMinDifferent(radarrValue)
            .Should().Be(isDifferent);
    }

    [Test]
    public void AnnotatedMax_OutsideThreshold_EqualsSameValueWithUnlimited()
    {
        const decimal testVal = QualityItem.MaxUnlimitedThreshold;
        var data = new QualityItem("", 0, testVal);
        data.AnnotatedMax.Should().Be($"{testVal} (Unlimited)");
    }

    [Test]
    public void AnnotatedMax_WithinThreshold_EqualsSameStringValue()
    {
        const decimal testVal = QualityItem.MaxUnlimitedThreshold - 1;
        var data = new QualityItem("", 0, testVal);
        data.AnnotatedMax.Should().Be($"{testVal}");
    }

    [Test]
    public void AnnotatedMin_NoThreshold_EqualsSameValue()
    {
        const decimal testVal = 10m;
        var data = new QualityItem("", 0, testVal);
        data.AnnotatedMax.Should().Be($"{testVal}");
    }

    [Test]
    public void Max_AboveThreshold_EqualsSameValue()
    {
        const decimal testVal = QualityItem.MaxUnlimitedThreshold + 1;
        var data = new QualityItem("", 0, testVal);
        data.Max.Should().Be(testVal);
    }

    [Test]
    public void MaxForApi_AboveThreshold_EqualsNull()
    {
        const decimal testVal = QualityItem.MaxUnlimitedThreshold + 1;
        var data = new QualityItem("", 0, testVal);
        data.MaxForApi.Should().Be(null);
    }

    [Test]
    public void MaxForApi_HighestWithinThreshold_EqualsSameValue()
    {
        const decimal testVal = QualityItem.MaxUnlimitedThreshold - 0.1m;
        var data = new QualityItem("", 0, testVal);
        data.MaxForApi.Should().Be(testVal).And.Be(data.Max);
    }

    [Test]
    public void MaxForApi_LowestWithinThreshold_EqualsSameValue()
    {
        var data = new QualityItem("", 0, 0);
        data.MaxForApi.Should().Be(0);
    }
}
