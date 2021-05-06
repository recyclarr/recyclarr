using FluentAssertions;
using NUnit.Framework;
using Trash.Radarr.QualityDefinition;

namespace Trash.Tests.Radarr.QualityDefinition
{
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
            new object?[] {RadarrQualityData.PreferredUnlimitedThreshold, null, false},
            new object?[] {RadarrQualityData.PreferredUnlimitedThreshold - 1, null, true},
            new object?[]
                {RadarrQualityData.PreferredUnlimitedThreshold, RadarrQualityData.PreferredUnlimitedThreshold, true}
        };

        [TestCaseSource(nameof(PreferredTestValues))]
        public void PreferredDifferent_WithVariousValues_ReturnsExpectedResult(decimal guideValue, decimal? radarrValue,
            bool isDifferent)
        {
            var data = new RadarrQualityData {Preferred = guideValue};
            data.IsPreferredDifferent(radarrValue)
                .Should().Be(isDifferent);
        }

        private static readonly object[] InterpolatedPreferredTestParams =
        {
            new[]
            {
                400m,
                1.0m,
                RadarrQualityData.PreferredUnlimitedThreshold
            },
            new[]
            {
                RadarrQualityData.PreferredUnlimitedThreshold,
                1.0m,
                RadarrQualityData.PreferredUnlimitedThreshold
            },
            new[]
            {
                RadarrQualityData.PreferredUnlimitedThreshold - 1m,
                1.0m,
                RadarrQualityData.PreferredUnlimitedThreshold - 1m
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
            var data = new RadarrQualityData {Min = 0, Max = max};
            data.InterpolatedPreferred(ratio).Should().Be(expectedResult);
        }

        [Test]
        public void AnnotatedPreferred_OutsideThreshold_EqualsSameValueWithUnlimited()
        {
            const decimal testVal = RadarrQualityData.PreferredUnlimitedThreshold;
            var data = new RadarrQualityData {Preferred = testVal};
            data.AnnotatedPreferred.Should().Be($"{testVal} (Unlimited)");
        }

        [Test]
        public void AnnotatedPreferred_WithinThreshold_EqualsSameStringValue()
        {
            const decimal testVal = RadarrQualityData.PreferredUnlimitedThreshold - 1;
            var data = new RadarrQualityData {Preferred = testVal};
            data.AnnotatedPreferred.Should().Be($"{testVal}");
        }

        [Test]
        public void Preferred_AboveThreshold_EqualsSameValue()
        {
            const decimal testVal = RadarrQualityData.PreferredUnlimitedThreshold + 1;
            var data = new RadarrQualityData {Preferred = testVal};
            data.Preferred.Should().Be(testVal);
        }

        [Test]
        public void PreferredForApi_AboveThreshold_EqualsNull()
        {
            const decimal testVal = RadarrQualityData.PreferredUnlimitedThreshold + 1;
            var data = new RadarrQualityData {Preferred = testVal};
            data.PreferredForApi.Should().Be(null);
        }

        [Test]
        public void PreferredForApi_HighestWithinThreshold_EqualsSameValue()
        {
            const decimal testVal = RadarrQualityData.PreferredUnlimitedThreshold - 0.1m;
            var data = new RadarrQualityData {Preferred = testVal};
            data.PreferredForApi.Should().Be(testVal).And.Be(data.Preferred);
        }

        [Test]
        public void PreferredForApi_LowestWithinThreshold_EqualsSameValue()
        {
            var data = new RadarrQualityData {Preferred = 0};
            data.PreferredForApi.Should().Be(0);
        }
    }
}
