using FluentValidation.TestHelper;
using NUnit.Framework;
using Recyclarr.Cli.TestLibrary;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Services.Sonarr.Config;

namespace Recyclarr.TrashLib.Tests.Sonarr;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class SonarrConfigurationValidatorTest : IntegrationFixture
{
    [Test]
    public void No_validation_failure_for_service_name()
    {
        var config = new SonarrConfiguration();

        var validator = Resolve<SonarrConfigurationValidator>();
        var result = validator.TestValidate(config);

        result.ShouldNotHaveValidationErrorFor(x => x.ServiceName);
    }

    [Test]
    public void Validation_failure_when_rps_and_cfs_used_together()
    {
        var config = new SonarrConfiguration
        {
            ReleaseProfiles = new[] {new ReleaseProfileConfig()},
            CustomFormats = new[] {new CustomFormatConfig()}
        };

        var validator = Resolve<SonarrConfigurationValidator>();
        var result = validator.TestValidate(config);

        result.ShouldHaveValidationErrorFor(x => x.ReleaseProfiles);
    }

    [Test]
    public void Sonarr_release_profile_failures()
    {
        var config = new SonarrConfiguration
        {
            ReleaseProfiles = new List<ReleaseProfileConfig>
            {
                new()
                {
                    TrashIds = Array.Empty<string>(),
                    Filter = new SonarrProfileFilterConfig
                    {
                        Include = new[] {"include"},
                        Exclude = new[] {"exclude"}
                    }
                }
            }
        };

        var validator = new SonarrConfigurationValidator();
        var result = validator.TestValidate(config);

        var releaseProfiles = $"{nameof(config.ReleaseProfiles)}[0].";

        // Release profile trash IDs cannot be empty
        result.ShouldHaveValidationErrorFor(releaseProfiles + nameof(ReleaseProfileConfig.TrashIds));

        // Cannot use include + exclude filters together
        result.ShouldHaveValidationErrorFor(releaseProfiles +
            $"{nameof(ReleaseProfileConfig.Filter)}.{nameof(SonarrProfileFilterConfig.Include)}");
    }
}
