using Recyclarr.TrashLib.Compatibility.Sonarr;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.ExceptionTypes;
using Recyclarr.TrashLib.TestLibrary;

namespace Recyclarr.TrashLib.Tests.Compatibility.Sonarr;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class SonarrCapabilityEnforcerTest
{
    [Test, AutoMockData]
    public void Fail_when_capabilities_not_obtained(
        [Frozen] ISonarrCapabilityChecker checker,
        SonarrCapabilityEnforcer sut)
    {
        var config = NewConfig.Sonarr();

        checker.GetCapabilities(default!).ReturnsForAnyArgs((SonarrCapabilities?) null);

        var act = () => sut.Check(config);

        act.Should().ThrowAsync<ServiceIncompatibilityException>().WithMessage("*obtained*");
    }

    [Test, AutoMockData]
    public void Minimum_version_not_met(
        [Frozen] ISonarrCapabilityChecker checker,
        SonarrCapabilityEnforcer sut)
    {
        var config = NewConfig.Sonarr();

        checker.GetCapabilities(default!).ReturnsForAnyArgs(new SonarrCapabilities(new Version())
        {
            SupportsNamedReleaseProfiles = false
        });

        var act = () => sut.Check(config);

        act.Should().ThrowAsync<ServiceIncompatibilityException>().WithMessage("*minimum*");
    }

    [Test, AutoMockData]
    public void Release_profiles_not_allowed_in_v4(
        [Frozen] ISonarrCapabilityChecker checker,
        SonarrCapabilityEnforcer sut)
    {
        var config = NewConfig.Sonarr() with
        {
            ReleaseProfiles = new List<ReleaseProfileConfig>
            {
                new()
            }
        };

        checker.GetCapabilities(default!).ReturnsForAnyArgs(new SonarrCapabilities(new Version())
        {
            SupportsNamedReleaseProfiles = true,
            SupportsCustomFormats = true
        });

        var act = () => sut.Check(config);

        act.Should().ThrowAsync<ServiceIncompatibilityException>().WithMessage("*v3*");
    }

    [Test, AutoMockData]
    public void Custom_formats_not_allowed_in_v3(
        [Frozen] ISonarrCapabilityChecker checker,
        SonarrCapabilityEnforcer sut)
    {
        var config = NewConfig.Sonarr() with
        {
            CustomFormats = new List<CustomFormatConfig>
            {
                new()
            }
        };

        checker.GetCapabilities(default!).ReturnsForAnyArgs(new SonarrCapabilities(new Version())
        {
            SupportsNamedReleaseProfiles = true,
            SupportsCustomFormats = false
        });

        var act = () => sut.Check(config);

        act.Should().ThrowAsync<ServiceIncompatibilityException>().WithMessage("*v4*");
    }
}
