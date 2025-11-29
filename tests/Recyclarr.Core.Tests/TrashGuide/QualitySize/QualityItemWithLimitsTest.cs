using Recyclarr.Core.TestLibrary;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Core.Tests.TrashGuide.QualitySize;

internal sealed class QualityItemWithLimitsTest
{
    private static readonly object[] MaxTestValues =
    [
        new object?[] { 100m, 100m, false },
        new object?[] { 100m, 101m, true },
        new object?[] { 100m, 98m, true },
        new object?[] { 100m, null, true },
        new object?[] { TestQualityItemLimits.MaxUnlimitedThreshold, null, false },
        new object?[] { TestQualityItemLimits.MaxUnlimitedThreshold - 1, null, true },
        new object?[]
        {
            TestQualityItemLimits.MaxUnlimitedThreshold,
            TestQualityItemLimits.MaxUnlimitedThreshold,
            true,
        },
        new object?[] { 399m, null, true },
    ];

    private static readonly object[] MinTestValues =
    [
        new object?[] { 0m, 0m, false },
        new object?[] { 0m, -1m, true },
        new object?[] { 0m, 1m, true },
    ];

    [TestCaseSource(nameof(MaxTestValues))]
    public void MaxDifferent_WithVariousValues_ReturnsExpectedResult(
        decimal guideValue,
        decimal? serviceValue,
        bool isDifferent
    )
    {
        var data = NewQualitySize.WithLimits("", 0, guideValue, 0);
        data.IsMaxDifferent(serviceValue).Should().Be(isDifferent);
    }

    [TestCaseSource(nameof(MinTestValues))]
    public void MinDifferent_WithVariousValues_ReturnsExpectedResult(
        decimal guideValue,
        decimal serviceValue,
        bool isDifferent
    )
    {
        var data = NewQualitySize.WithLimits("", guideValue, 0, 0);
        data.IsMinDifferent(serviceValue).Should().Be(isDifferent);
    }

    [Test]
    public void AnnotatedMax_OutsideThreshold_EqualsSameValueWithUnlimited()
    {
        const decimal testVal = TestQualityItemLimits.MaxUnlimitedThreshold;
        var data = NewQualitySize.WithLimits("", 0, testVal, 0);
        data.AnnotatedMax.Should().Be($"{testVal} (Unlimited)");
    }

    [Test]
    public void AnnotatedMax_WithinThreshold_EqualsSameStringValue()
    {
        const decimal testVal = TestQualityItemLimits.MaxUnlimitedThreshold - 1;
        var data = NewQualitySize.WithLimits("", 0, testVal, 0);
        data.AnnotatedMax.Should().Be($"{testVal}");
    }

    [Test]
    public void AnnotatedMin_NoThreshold_EqualsSameValue()
    {
        const decimal testVal = 10m;
        var data = NewQualitySize.WithLimits("", 0, testVal, 0);
        data.AnnotatedMax.Should().Be($"{testVal}");
    }

    [Test]
    public void MaxForApi_AboveThreshold_EqualsNull()
    {
        const decimal testVal = TestQualityItemLimits.MaxUnlimitedThreshold + 1;
        var data = NewQualitySize.WithLimits("", 0, testVal, 0);
        data.MaxForApi.Should().Be(null);
    }

    [Test]
    public void MaxForApi_HighestWithinThreshold_EqualsSameValue()
    {
        const decimal testVal = TestQualityItemLimits.MaxUnlimitedThreshold - 0.1m;
        var data = NewQualitySize.WithLimits("", 0, testVal, 0);
        data.MaxForApi.Should().Be(testVal).And.Be(data.Item.Max);
    }

    [Test]
    public void MaxForApi_LowestWithinThreshold_EqualsSameValue()
    {
        var data = NewQualitySize.WithLimits("", 0, 0, 0);
        data.MaxForApi.Should().Be(0);
    }

    private static readonly object[] PreferredTestValues =
    [
        new object?[] { 100m, 100m, false },
        new object?[] { 100m, 101m, true },
        new object?[] { 100m, 98m, true },
        new object?[] { 100m, null, true },
        new object?[] { TestQualityItemLimits.PreferredUnlimitedThreshold, null, false },
        new object?[] { TestQualityItemLimits.PreferredUnlimitedThreshold - 1, null, true },
        new object?[]
        {
            TestQualityItemLimits.PreferredUnlimitedThreshold,
            TestQualityItemLimits.PreferredUnlimitedThreshold,
            true,
        },
    ];

    [TestCaseSource(nameof(PreferredTestValues))]
    public void PreferredDifferent_WithVariousValues_ReturnsExpectedResult(
        decimal guideValue,
        decimal? serviceValue,
        bool isDifferent
    )
    {
        var data = NewQualitySize.WithLimits("", 0, 0, guideValue);
        data.IsPreferredDifferent(serviceValue).Should().Be(isDifferent);
    }

    private static readonly object[] InterpolatedPreferredTestParams =
    [
        new[] { 400m, 1.0m, TestQualityItemLimits.PreferredUnlimitedThreshold },
        new[]
        {
            TestQualityItemLimits.PreferredUnlimitedThreshold,
            1.0m,
            TestQualityItemLimits.PreferredUnlimitedThreshold,
        },
        new[]
        {
            TestQualityItemLimits.PreferredUnlimitedThreshold - 1m,
            1.0m,
            TestQualityItemLimits.PreferredUnlimitedThreshold - 1m,
        },
        new[] { 10m, 0m, 0m },
        new[] { 100m, 0.5m, 50m },
    ];

    [TestCaseSource(nameof(InterpolatedPreferredTestParams))]
    public void InterpolatedPreferred_VariousValues_ExpectedResults(
        decimal max,
        decimal ratio,
        decimal expectedResult
    )
    {
        var data = NewQualitySize.WithLimits("", 0, max, 0);
        data.InterpolatedPreferred(ratio).Should().Be(expectedResult);
    }

    [Test]
    public void AnnotatedPreferred_OutsideThreshold_EqualsSameValueWithUnlimited()
    {
        const decimal testVal = TestQualityItemLimits.PreferredUnlimitedThreshold;
        var data = NewQualitySize.WithLimits("", 0, 0, testVal);
        data.AnnotatedPreferred.Should().Be($"{testVal} (Unlimited)");
    }

    [Test]
    public void AnnotatedPreferred_WithinThreshold_EqualsSameStringValue()
    {
        const decimal testVal = TestQualityItemLimits.PreferredUnlimitedThreshold - 1;
        var data = NewQualitySize.WithLimits("", 0, 0, testVal);
        data.AnnotatedPreferred.Should().Be($"{testVal}");
    }

    [Test]
    public void PreferredForApi_AboveThreshold_EqualsNull()
    {
        const decimal testVal = TestQualityItemLimits.PreferredUnlimitedThreshold + 1;
        var data = NewQualitySize.WithLimits("", 0, 0, testVal);
        data.PreferredForApi.Should().Be(null);
    }

    [Test]
    public void PreferredForApi_HighestWithinThreshold_EqualsSameValue()
    {
        const decimal testVal = TestQualityItemLimits.PreferredUnlimitedThreshold - 0.1m;
        var data = NewQualitySize.WithLimits("", 0, 0, testVal);
        data.PreferredForApi.Should().Be(testVal).And.Be(data.Item.Preferred);
    }

    [Test]
    public void PreferredForApi_LowestWithinThreshold_EqualsSameValue()
    {
        var data = NewQualitySize.WithLimits("", 0, 0, 0);
        data.PreferredForApi.Should().Be(0);
    }

    [Test]
    public void Max_and_preferred_are_capped_when_over_limit()
    {
        var sut = new QualityItemWithLimits(
            NewQualitySize.Item("TestQuality", 10m, 100m, 100m),
            new QualityItemLimits(50m, 70m)
        );

        sut.Item.Should().BeEquivalentTo(NewQualitySize.Item("TestQuality", 10m, 50m, 70m));
    }
}
