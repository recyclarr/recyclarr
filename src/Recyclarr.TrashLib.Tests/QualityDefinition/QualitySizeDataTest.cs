using FluentAssertions;
using NUnit.Framework;
using Recyclarr.TrashLib.Services.QualitySize;

namespace Recyclarr.TrashLib.Tests.QualityDefinition;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class QualitySizeDataTest
{
    private static readonly object[] PreferredTestValues =
    {
        new object?[] {100m, 100m, false},
        new object?[] {100m, 101m, true},
        new object?[] {100m, 98m, true},
        new object?[] {100m, null, true},
        new object?[] {QualitySizeItem.PreferredUnlimitedThreshold, null, false},
        new object?[] {QualitySizeItem.PreferredUnlimitedThreshold - 1, null, true},
        new object?[]
            {QualitySizeItem.PreferredUnlimitedThreshold, QualitySizeItem.PreferredUnlimitedThreshold, true}
    };

    [TestCaseSource(nameof(PreferredTestValues))]
    public void PreferredDifferent_WithVariousValues_ReturnsExpectedResult(decimal guideValue, decimal? radarrValue,
        bool isDifferent)
    {
        var data = new QualitySizeItem("", 0, 0, guideValue);
        data.IsPreferredDifferent(radarrValue)
            .Should().Be(isDifferent);
    }

    private static readonly object[] InterpolatedPreferredTestParams =
    {
        new[]
        {
            400m,
            1.0m,
            QualitySizeItem.PreferredUnlimitedThreshold
        },
        new[]
        {
            QualitySizeItem.PreferredUnlimitedThreshold,
            1.0m,
            QualitySizeItem.PreferredUnlimitedThreshold
        },
        new[]
        {
            QualitySizeItem.PreferredUnlimitedThreshold - 1m,
            1.0m,
            QualitySizeItem.PreferredUnlimitedThreshold - 1m
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
        var data = new QualitySizeItem("", 0, max, 0);
        data.InterpolatedPreferred(ratio).Should().Be(expectedResult);
    }

    [Test]
    public void AnnotatedPreferred_OutsideThreshold_EqualsSameValueWithUnlimited()
    {
        const decimal testVal = QualitySizeItem.PreferredUnlimitedThreshold;
        var data = new QualitySizeItem("", 0, 0, testVal);
        data.AnnotatedPreferred.Should().Be($"{testVal} (Unlimited)");
    }

    [Test]
    public void AnnotatedPreferred_WithinThreshold_EqualsSameStringValue()
    {
        const decimal testVal = QualitySizeItem.PreferredUnlimitedThreshold - 1;
        var data = new QualitySizeItem("", 0, 0, testVal);
        data.AnnotatedPreferred.Should().Be($"{testVal}");
    }

    [Test]
    public void Preferred_AboveThreshold_EqualsSameValue()
    {
        const decimal testVal = QualitySizeItem.PreferredUnlimitedThreshold + 1;
        var data = new QualitySizeItem("", 0, 0, testVal);
        data.Preferred.Should().Be(testVal);
    }

    [Test]
    public void PreferredForApi_AboveThreshold_EqualsNull()
    {
        const decimal testVal = QualitySizeItem.PreferredUnlimitedThreshold + 1;
        var data = new QualitySizeItem("", 0, 0, testVal);
        data.PreferredForApi.Should().Be(null);
    }

    [Test]
    public void PreferredForApi_HighestWithinThreshold_EqualsSameValue()
    {
        const decimal testVal = QualitySizeItem.PreferredUnlimitedThreshold - 0.1m;
        var data = new QualitySizeItem("", 0, 0, testVal);
        data.PreferredForApi.Should().Be(testVal).And.Be(data.Preferred);
    }

    [Test]
    public void PreferredForApi_LowestWithinThreshold_EqualsSameValue()
    {
        var data = new QualitySizeItem("", 0, 0, 0);
        data.PreferredForApi.Should().Be(0);
    }
}
