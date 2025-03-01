using Recyclarr.Compatibility;
using Recyclarr.Compatibility.Sonarr;

namespace Recyclarr.Core.Tests.Compatibility.Sonarr;

[TestFixture]
public class SonarrCapabilityEnforcerTest
{
    [Test, AutoMockData]
    public void Minimum_version_not_met(
        [Frozen] ISonarrCapabilityFetcher fetcher,
        SonarrCapabilityEnforcer sut
    )
    {
        var min = SonarrCapabilities.MinimumVersion;

        fetcher
            .GetCapabilities(CancellationToken.None)
            .ReturnsForAnyArgs(
                new SonarrCapabilities(
                    new Version(min.Major - 1, min.Minor, min.Build, min.Revision)
                )
            );

        var act = () => sut.Check(CancellationToken.None);

        act.Should().ThrowAsync<ServiceIncompatibilityException>().WithMessage("*minimum*");
    }
}
