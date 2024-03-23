using Recyclarr.Compatibility;
using Recyclarr.Compatibility.Sonarr;
using Recyclarr.Tests.TestLibrary;

namespace Recyclarr.Tests.Compatibility.Sonarr;

[TestFixture]
public class SonarrCapabilityEnforcerTest
{
    [Test, AutoMockData]
    public void Minimum_version_not_met(
        [Frozen] ISonarrCapabilityFetcher fetcher,
        SonarrCapabilityEnforcer sut)
    {
        var config = NewConfig.Sonarr();
        var min = SonarrCapabilities.MinimumVersion;

        fetcher.GetCapabilities(default!).ReturnsForAnyArgs(
            new SonarrCapabilities(new Version(min.Major - 1, min.Minor, min.Build, min.Revision)));

        var act = () => sut.Check(config);

        act.Should().ThrowAsync<ServiceIncompatibilityException>().WithMessage("*minimum*");
    }
}
