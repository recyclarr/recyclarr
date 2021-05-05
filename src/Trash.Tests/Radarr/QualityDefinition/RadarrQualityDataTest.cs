using FluentAssertions;
using NUnit.Framework;
using Trash.Radarr.QualityDefinition;

namespace Trash.Tests.Radarr.QualityDefinition
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class RadarrQualityDataTest
    {
        private static readonly object[] ToleranceTestValues =
        {
            new object[] {-RadarrQualityData.Tolerance - 0.01m, true},
            new object[] {-RadarrQualityData.Tolerance, false},
            new object[] {-RadarrQualityData.Tolerance / 2, false},
            new object[] {RadarrQualityData.Tolerance / 2, false},
            new object[] {RadarrQualityData.Tolerance, false},
            new object[] {RadarrQualityData.Tolerance + 0.01m, true}
        };

        [TestCaseSource(nameof(ToleranceTestValues))]
        public void PreferredOutsideTolerance_WithVariousTolerance_ReturnsExpectedResult(decimal offset,
            bool expectedResult)
        {
            const decimal testVal = 100;
            var data = new RadarrQualityData {Preferred = testVal};
            data.PreferredOutsideTolerance(testVal + offset)
                .Should().Be(expectedResult);
        }

        [TestCaseSource(nameof(ToleranceTestValues))]
        public void MaxOutsideTolerance_WithVariousTolerance_ReturnsExpectedResult(decimal offset, bool expectedResult)
        {
            const decimal testVal = 100;
            var data = new RadarrQualityData {Max = testVal};
            data.MaxOutsideTolerance(testVal + offset)
                .Should().Be(expectedResult);
        }

        [TestCaseSource(nameof(ToleranceTestValues))]
        public void MinOutsideTolerance_WithVariousTolerance_ReturnsExpectedResult(decimal offset, bool expectedResult)
        {
            const decimal testVal = 0;
            var data = new RadarrQualityData {Min = testVal};
            data.MinOutsideTolerance(testVal + offset)
                .Should().Be(expectedResult);
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
        public void AnnotatedMax_OutsideThreshold_EqualsSameValueWithUnlimited()
        {
            const decimal testVal = RadarrQualityData.MaxUnlimitedThreshold;
            var data = new RadarrQualityData {Max = testVal};
            data.AnnotatedMax.Should().Be($"{testVal} (Unlimited)");
        }

        [Test]
        public void AnnotatedMax_WithinThreshold_EqualsSameStringValue()
        {
            const decimal testVal = RadarrQualityData.MaxUnlimitedThreshold - 1;
            var data = new RadarrQualityData {Max = testVal};
            data.AnnotatedMax.Should().Be($"{testVal}");
        }

        [Test]
        public void AnnotatedMin_NoThreshold_EqualsSameValue()
        {
            const decimal testVal = 10m;
            var data = new RadarrQualityData {Max = testVal};
            data.AnnotatedMax.Should().Be($"{testVal}");
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
        public void Max_AboveThreshold_EqualsSameValue()
        {
            const decimal testVal = RadarrQualityData.MaxUnlimitedThreshold + 1;
            var data = new RadarrQualityData {Max = testVal};
            data.Max.Should().Be(testVal);
        }

        [Test]
        public void MaxForApi_AboveThreshold_EqualsNull()
        {
            const decimal testVal = RadarrQualityData.MaxUnlimitedThreshold + 1;
            var data = new RadarrQualityData {Max = testVal};
            data.MaxForApi.Should().Be(null);
        }

        [Test]
        public void MaxForApi_HighestWithinThreshold_EqualsSameValue()
        {
            const decimal testVal = RadarrQualityData.MaxUnlimitedThreshold - 0.1m;
            var data = new RadarrQualityData {Max = testVal};
            data.MaxForApi.Should().Be(testVal).And.Be(data.Max);
        }

        [Test]
        public void MaxForApi_LowestWithinThreshold_EqualsSameValue()
        {
            var data = new RadarrQualityData {Max = 0};
            data.MaxForApi.Should().Be(0);
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
