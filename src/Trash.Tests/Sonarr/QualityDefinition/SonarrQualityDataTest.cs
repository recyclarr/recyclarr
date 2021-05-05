using FluentAssertions;
using NUnit.Framework;
using Trash.Sonarr.QualityDefinition;

namespace Trash.Tests.Sonarr.QualityDefinition
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class SonarrQualityDataTest
    {
        private static readonly object[] ToleranceTestValues =
        {
            new object[] {-SonarrQualityData.Tolerance - 0.01m, true},
            new object[] {-SonarrQualityData.Tolerance, false},
            new object[] {-SonarrQualityData.Tolerance / 2, false},
            new object[] {SonarrQualityData.Tolerance / 2, false},
            new object[] {SonarrQualityData.Tolerance, false},
            new object[] {SonarrQualityData.Tolerance + 0.01m, true}
        };

        [TestCaseSource(nameof(ToleranceTestValues))]
        public void MaxOutsideTolerance_WithVariousTolerance_ReturnsExpectedResult(decimal offset, bool expectedResult)
        {
            const decimal testVal = 100;
            var data = new SonarrQualityData {Max = testVal};
            data.MaxOutsideTolerance(testVal + offset)
                .Should().Be(expectedResult);
        }

        [TestCaseSource(nameof(ToleranceTestValues))]
        public void MinOutsideTolerance_WithVariousTolerance_ReturnsExpectedResult(decimal offset, bool expectedResult)
        {
            const decimal testVal = 0;
            var data = new SonarrQualityData {Min = testVal};
            data.MinOutsideTolerance(testVal + offset)
                .Should().Be(expectedResult);
        }

        [Test]
        public void AnnotatedMax_OutsideThreshold_EqualsSameValueWithUnlimited()
        {
            const decimal testVal = SonarrQualityData.MaxUnlimitedThreshold;
            var data = new SonarrQualityData {Max = testVal};
            data.AnnotatedMax.Should().Be($"{testVal} (Unlimited)");
        }

        [Test]
        public void AnnotatedMax_WithinThreshold_EqualsSameStringValue()
        {
            const decimal testVal = SonarrQualityData.MaxUnlimitedThreshold - 1;
            var data = new SonarrQualityData {Max = testVal};
            data.AnnotatedMax.Should().Be($"{testVal}");
        }

        [Test]
        public void AnnotatedMin_NoThreshold_EqualsSameValue()
        {
            const decimal testVal = 10m;
            var data = new SonarrQualityData {Max = testVal};
            data.AnnotatedMax.Should().Be($"{testVal}");
        }

        [Test]
        public void Max_AboveThreshold_EqualsSameValue()
        {
            const decimal testVal = SonarrQualityData.MaxUnlimitedThreshold + 1;
            var data = new SonarrQualityData {Max = testVal};
            data.Max.Should().Be(testVal);
        }

        [Test]
        public void MaxForApi_AboveThreshold_EqualsNull()
        {
            const decimal testVal = SonarrQualityData.MaxUnlimitedThreshold + 1;
            var data = new SonarrQualityData {Max = testVal};
            data.MaxForApi.Should().Be(null);
        }

        [Test]
        public void MaxForApi_HighestWithinThreshold_EqualsSameValue()
        {
            const decimal testVal = SonarrQualityData.MaxUnlimitedThreshold - 0.1m;
            var data = new SonarrQualityData {Max = testVal};
            data.MaxForApi.Should().Be(testVal).And.Be(data.Max);
        }

        [Test]
        public void MaxForApi_LowestWithinThreshold_EqualsSameValue()
        {
            var data = new SonarrQualityData {Max = 0};
            data.MaxForApi.Should().Be(0);
        }
    }
}
