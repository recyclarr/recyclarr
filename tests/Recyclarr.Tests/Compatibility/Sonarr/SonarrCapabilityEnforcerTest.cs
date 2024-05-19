using Recyclarr.Compatibility;
using Recyclarr.Compatibility.Sonarr;

namespace Recyclarr.Tests.Compatibility.Sonarr;

[TestFixture]
public class SonarrCapabilityEnforcerTest
{
    [Test, AutoMockData]
    public void Minimum_version_not_met(
        [Frozen] ISonarrCapabilityFetcher fetcher,
        SonarrCapabilityEnforcer sut)
    {
        var min = SonarrCapabilities.MinimumVersion;

        fetcher.GetCapabilities().ReturnsForAnyArgs(
            new SonarrCapabilities(new Version(min.Major - 1, min.Minor, min.Build, min.Revision)));

        var act = sut.Check;

        act.Should().ThrowAsync<ServiceIncompatibilityException>().WithMessage("*minimum*");
    }
}
