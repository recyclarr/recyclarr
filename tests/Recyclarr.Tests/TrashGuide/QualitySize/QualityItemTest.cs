using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Tests.TrashGuide.QualitySize;

[TestFixture]
public class QualityItemTest
{
    private static readonly object[] MaxTestValues =
    [
        new object?[] {100m, 100m, false},
        new object?[] {100m, 101m, true},
        new object?[] {100m, 98m, true},
        new object?[] {100m, null, true},
        new object?[] {QualityItem.MaxUnlimitedThreshold, null, false},
        new object?[] {QualityItem.MaxUnlimitedThreshold - 1, null, true},
        new object?[] {QualityItem.MaxUnlimitedThreshold, QualityItem.MaxUnlimitedThreshold, true}
    ];

    private static readonly object[] MinTestValues =
    [
        new object?[] {0m, 0m, false},
        new object?[] {0m, -1m, true},
        new object?[] {0m, 1m, true}
    ];

    [TestCaseSource(nameof(MaxTestValues))]
    public void MaxDifferent_WithVariousValues_ReturnsExpectedResult(
        decimal guideValue,
        decimal? radarrValue,
        bool isDifferent)
    {
        var data = new QualityItem("", 0, guideValue, 0);
        data.IsMaxDifferent(radarrValue)
            .Should().Be(isDifferent);
    }

    [TestCaseSource(nameof(MinTestValues))]
    public void MinDifferent_WithVariousValues_ReturnsExpectedResult(
        decimal guideValue,
        decimal radarrValue,
        bool isDifferent)
    {
        var data = new QualityItem("", guideValue, 0, 0);
        data.IsMinDifferent(radarrValue)
            .Should().Be(isDifferent);
    }

    [Test]
    public void AnnotatedMax_OutsideThreshold_EqualsSameValueWithUnlimited()
    {
        const decimal testVal = QualityItem.MaxUnlimitedThreshold;
        var data = new QualityItem("", 0, testVal, 0);
        data.AnnotatedMax.Should().Be($"{testVal} (Unlimited)");
    }

    [Test]
    public void AnnotatedMax_WithinThreshold_EqualsSameStringValue()
    {
        const decimal testVal = QualityItem.MaxUnlimitedThreshold - 1;
        var data = new QualityItem("", 0, testVal, 0);
        data.AnnotatedMax.Should().Be($"{testVal}");
    }

    [Test]
    public void AnnotatedMin_NoThreshold_EqualsSameValue()
    {
        const decimal testVal = 10m;
        var data = new QualityItem("", 0, testVal, 0);
        data.AnnotatedMax.Should().Be($"{testVal}");
    }

    [Test]
    public void Max_AboveThreshold_EqualsSameValue()
    {
        const decimal testVal = QualityItem.MaxUnlimitedThreshold + 1;
        var data = new QualityItem("", 0, testVal, 0);
        data.Max.Should().Be(testVal);
    }

    [Test]
    public void MaxForApi_AboveThreshold_EqualsNull()
    {
        const decimal testVal = QualityItem.MaxUnlimitedThreshold + 1;
        var data = new QualityItem("", 0, testVal, 0);
        data.MaxForApi.Should().Be(null);
    }

    [Test]
    public void MaxForApi_HighestWithinThreshold_EqualsSameValue()
    {
        const decimal testVal = QualityItem.MaxUnlimitedThreshold - 0.1m;
        var data = new QualityItem("", 0, testVal, 0);
        data.MaxForApi.Should().Be(testVal).And.Be(data.Max);
    }

    [Test]
    public void MaxForApi_LowestWithinThreshold_EqualsSameValue()
    {
        var data = new QualityItem("", 0, 0, 0);
        data.MaxForApi.Should().Be(0);
    }

    private static readonly object[] PreferredTestValues =
    [
        new object?[] {100m, 100m, false},
        new object?[] {100m, 101m, true},
        new object?[] {100m, 98m, true},
        new object?[] {100m, null, true},
        new object?[] {QualityItem.PreferredUnlimitedThreshold, null, false},
        new object?[] {QualityItem.PreferredUnlimitedThreshold - 1, null, true},
        new object?[]
        {
            QualityItem.PreferredUnlimitedThreshold, QualityItem.PreferredUnlimitedThreshold,
            true
        }
    ];

    [TestCaseSource(nameof(PreferredTestValues))]
    public void PreferredDifferent_WithVariousValues_ReturnsExpectedResult(
        decimal guideValue,
        decimal? radarrValue,
        bool isDifferent)
    {
        var data = new QualityItem("", 0, 0, guideValue);
        data.IsPreferredDifferent(radarrValue)
            .Should().Be(isDifferent);
    }

    private static readonly object[] InterpolatedPreferredTestParams =
    [
        new[]
        {
            400m,
            1.0m,
            QualityItem.PreferredUnlimitedThreshold
        },
        new[]
        {
            QualityItem.PreferredUnlimitedThreshold,
            1.0m,
            QualityItem.PreferredUnlimitedThreshold
        },
        new[]
        {
            QualityItem.PreferredUnlimitedThreshold - 1m,
            1.0m,
            QualityItem.PreferredUnlimitedThreshold - 1m
        },
        new[]
        {
            10m,
            0m,
            0m
        },
        new[]
        {
            100m,
            0.5m,
            50m
        }
    ];

    [TestCaseSource(nameof(InterpolatedPreferredTestParams))]
    public void InterpolatedPreferred_VariousValues_ExpectedResults(
        decimal max,
        decimal ratio,
        decimal expectedResult)
    {
        var data = new QualityItem("", 0, max, 0);
        data.InterpolatedPreferred(ratio).Should().Be(expectedResult);
    }

    [Test]
    public void AnnotatedPreferred_OutsideThreshold_EqualsSameValueWithUnlimited()
    {
        const decimal testVal = QualityItem.PreferredUnlimitedThreshold;
        var data = new QualityItem("", 0, 0, testVal);
        data.AnnotatedPreferred.Should().Be($"{testVal} (Unlimited)");
    }

    [Test]
    public void AnnotatedPreferred_WithinThreshold_EqualsSameStringValue()
    {
        const decimal testVal = QualityItem.PreferredUnlimitedThreshold - 1;
        var data = new QualityItem("", 0, 0, testVal);
        data.AnnotatedPreferred.Should().Be($"{testVal}");
    }

    [Test]
    public void Preferred_AboveThreshold_EqualsSameValue()
    {
        const decimal testVal = QualityItem.PreferredUnlimitedThreshold + 1;
        var data = new QualityItem("", 0, 0, testVal);
        data.Preferred.Should().Be(testVal);
    }

    [Test]
    public void PreferredForApi_AboveThreshold_EqualsNull()
    {
        const decimal testVal = QualityItem.PreferredUnlimitedThreshold + 1;
        var data = new QualityItem("", 0, 0, testVal);
        data.PreferredForApi.Should().Be(null);
    }

    [Test]
    public void PreferredForApi_HighestWithinThreshold_EqualsSameValue()
    {
        const decimal testVal = QualityItem.PreferredUnlimitedThreshold - 0.1m;
        var data = new QualityItem("", 0, 0, testVal);
        data.PreferredForApi.Should().Be(testVal).And.Be(data.Preferred);
    }

    [Test]
    public void PreferredForApi_LowestWithinThreshold_EqualsSameValue()
    {
        var data = new QualityItem("", 0, 0, 0);
        data.PreferredForApi.Should().Be(0);
    }
}
