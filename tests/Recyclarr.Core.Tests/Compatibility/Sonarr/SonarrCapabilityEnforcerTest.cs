using Recyclarr.Compatibility;
using Recyclarr.Compatibility.Sonarr;

namespace Recyclarr.Core.Tests.Compatibility.Sonarr;

internal sealed class SonarrCapabilityEnforcerTest
{
    [Test]
    public void Minimum_version_not_met()
    {
        var fetcher = Substitute.For<ISonarrCapabilityFetcher>();
        var sut = new SonarrCapabilityEnforcer(fetcher);
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
