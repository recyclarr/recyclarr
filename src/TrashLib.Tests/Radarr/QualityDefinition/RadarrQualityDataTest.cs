using FluentAssertions;
using NUnit.Framework;
using TrashLib.Services.Radarr.QualityDefinition;

namespace TrashLib.Tests.Radarr.QualityDefinition;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class RadarrQualityDataTest
{
    private static readonly object[] PreferredTestValues =
    {
        new object?[] {100m, 100m, false},
        new object?[] {100m, 101m, true},
        new object?[] {100m, 98m, true},
        new object?[] {100m, null, true},
        new object?[] {RadarrQualityItem.PreferredUnlimitedThreshold, null, false},
        new object?[] {RadarrQualityItem.PreferredUnlimitedThreshold - 1, null, true},
        new object?[]
            {RadarrQualityItem.PreferredUnlimitedThreshold, RadarrQualityItem.PreferredUnlimitedThreshold, true}
    };

    [TestCaseSource(nameof(PreferredTestValues))]
    public void PreferredDifferent_WithVariousValues_ReturnsExpectedResult(decimal guideValue, decimal? radarrValue,
        bool isDifferent)
    {
        var data = new RadarrQualityItem("", 0, 0, guideValue);
        data.IsPreferredDifferent(radarrValue)
            .Should().Be(isDifferent);
    }

    private static readonly object[] InterpolatedPreferredTestParams =
    {
        new[]
        {
            400m,
            1.0m,
            RadarrQualityItem.PreferredUnlimitedThreshold
        },
        new[]
        {
            RadarrQualityItem.PreferredUnlimitedThreshold,
            1.0m,
            RadarrQualityItem.PreferredUnlimitedThreshold
        },
        new[]
        {
            RadarrQualityItem.PreferredUnlimitedThreshold - 1m,
            1.0m,
            RadarrQualityItem.PreferredUnlimitedThreshold - 1m
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
    };

    [TestCaseSource(nameof(InterpolatedPreferredTestParams))]
    public void InterpolatedPreferred_VariousValues_ExpectedResults(decimal max, decimal ratio,
        decimal expectedResult)
    {
        var data = new RadarrQualityItem("", 0, max, 0);
        data.InterpolatedPreferred(ratio).Should().Be(expectedResult);
    }

    [Test]
    public void AnnotatedPreferred_OutsideThreshold_EqualsSameValueWithUnlimited()
    {
        const decimal testVal = RadarrQualityItem.PreferredUnlimitedThreshold;
        var data = new RadarrQualityItem("", 0, 0, testVal);
        data.AnnotatedPreferred.Should().Be($"{testVal} (Unlimited)");
    }

    [Test]
    public void AnnotatedPreferred_WithinThreshold_EqualsSameStringValue()
    {
        const decimal testVal = RadarrQualityItem.PreferredUnlimitedThreshold - 1;
        var data = new RadarrQualityItem("", 0, 0, testVal);
        data.AnnotatedPreferred.Should().Be($"{testVal}");
    }

    [Test]
    public void Preferred_AboveThreshold_EqualsSameValue()
    {
        const decimal testVal = RadarrQualityItem.PreferredUnlimitedThreshold + 1;
        var data = new RadarrQualityItem("", 0, 0, testVal);
        data.Preferred.Should().Be(testVal);
    }

    [Test]
    public void PreferredForApi_AboveThreshold_EqualsNull()
    {
        const decimal testVal = RadarrQualityItem.PreferredUnlimitedThreshold + 1;
        var data = new RadarrQualityItem("", 0, 0, testVal);
        data.PreferredForApi.Should().Be(null);
    }

    [Test]
    public void PreferredForApi_HighestWithinThreshold_EqualsSameValue()
    {
        const decimal testVal = RadarrQualityItem.PreferredUnlimitedThreshold - 0.1m;
        var data = new RadarrQualityItem("", 0, 0, testVal);
        data.PreferredForApi.Should().Be(testVal).And.Be(data.Preferred);
    }

    [Test]
    public void PreferredForApi_LowestWithinThreshold_EqualsSameValue()
    {
        var data = new RadarrQualityItem("", 0, 0, 0);
        data.PreferredForApi.Should().Be(0);
    }
}
