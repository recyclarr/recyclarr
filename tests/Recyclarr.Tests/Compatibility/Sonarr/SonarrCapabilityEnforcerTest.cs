using Recyclarr.Compatibility;
using Recyclarr.Compatibility.Sonarr;
using Recyclarr.Config.Models;
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

        fetcher.GetCapabilities(default!).ReturnsForAnyArgs(new SonarrCapabilities
        {
            Version = new Version(min.Major, min.Minor, min.Build, min.Revision - 1)
        });

        var act = () => sut.Check(config);

        act.Should().ThrowAsync<ServiceIncompatibilityException>().WithMessage("*minimum*");
    }

    [Test, AutoMockData]
    public void Release_profiles_not_allowed_in_v4(
        [Frozen] ISonarrCapabilityFetcher fetcher,
        SonarrCapabilityEnforcer sut)
    {
        var config = NewConfig.Sonarr() with
        {
            ReleaseProfiles = new List<ReleaseProfileConfig>
            {
                new()
            }
        };

        fetcher.GetCapabilities(default!).ReturnsForAnyArgs(new SonarrCapabilities
        {
            SupportsCustomFormats = true
        });

        var act = () => sut.Check(config);

        act.Should().ThrowAsync<ServiceIncompatibilityException>().WithMessage("*v3*");
    }

    [Test, AutoMockData]
    public void Custom_formats_not_allowed_in_v3(
        [Frozen] ISonarrCapabilityFetcher fetcher,
        SonarrCapabilityEnforcer sut)
    {
        var config = NewConfig.Sonarr() with
        {
            CustomFormats = new List<CustomFormatConfig>
            {
                new()
            }
        };

        fetcher.GetCapabilities(default!).ReturnsForAnyArgs(new SonarrCapabilities
        {
            SupportsCustomFormats = false
        });

        var act = () => sut.Check(config);

        act.Should().ThrowAsync<ServiceIncompatibilityException>().WithMessage("*custom formats*v4*");
    }

    [Test, AutoMockData]
    public void Qualities_not_allowed_in_v3(
        [Frozen] ISonarrCapabilityFetcher fetcher,
        SonarrCapabilityEnforcer sut)
    {
        var config = NewConfig.Sonarr() with
        {
            QualityProfiles = new[]
            {
                new QualityProfileConfig
                {
                    Qualities = new[]
                    {
                        new QualityProfileQualityConfig()
                    }
                }
            }
        };

        fetcher.GetCapabilities(default!).ReturnsForAnyArgs(new SonarrCapabilities
        {
            SupportsCustomFormats = false
        });

        var act = () => sut.Check(config);

        act.Should().ThrowAsync<ServiceIncompatibilityException>().WithMessage("*qualities*v4*");
    }
}
