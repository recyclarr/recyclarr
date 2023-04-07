using FluentValidation.TestHelper;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.Config.Data.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class SonarrConfigYamlValidatorTest
{
    [Test]
    public void Validation_failure_when_rps_and_cfs_used_together()
    {
        var config = new SonarrConfigYamlLatest
        {
            ReleaseProfiles = new[] {new ReleaseProfileConfigYamlLatest()},
            CustomFormats = new[] {new CustomFormatConfigYamlLatest()}
        };

        var validator = new SonarrConfigYamlValidatorLatest();
        var result = validator.TestValidate(config);

        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("`custom_formats` and `release_profiles` may not be used together");
    }

    [Test]
    public void Sonarr_release_profile_failures()
    {
        var config = new ReleaseProfileConfigYamlLatest
        {
            TrashIds = Array.Empty<string>(),
            Filter = new ReleaseProfileFilterConfigYamlLatest
            {
                Include = new[] {"include"},
                Exclude = new[] {"exclude"}
            }
        };

        var validator = new ReleaseProfileConfigYamlValidatorLatest();
        var result = validator.TestValidate(config);

        result.Errors.Should().HaveCount(2);

        // Release profile trash IDs cannot be empty
        result.ShouldHaveValidationErrorFor(x => x.TrashIds);

        // Cannot use include + exclude filters together
        result.ShouldHaveValidationErrorFor(nameof(ReleaseProfileConfig.Filter));
    }

    [Test]
    public void Filter_include_can_not_be_empty()
    {
        var config = new ReleaseProfileFilterConfigYamlLatest
        {
            Include = Array.Empty<string>(),
            Exclude = new[] {"exclude"}
        };

        var validator = new ReleaseProfileFilterConfigYamlValidatorLatest();
        var result = validator.TestValidate(config);

        result.Errors.Should().HaveCount(1);

        result.ShouldHaveValidationErrorFor(x => x.Include);
    }

    [Test]
    public void Filter_exclude_can_not_be_empty()
    {
        var config = new ReleaseProfileFilterConfigYamlLatest
        {
            Exclude = Array.Empty<string>(),
            Include = new[] {"exclude"}
        };

        var validator = new ReleaseProfileFilterConfigYamlValidatorLatest();
        var result = validator.TestValidate(config);

        result.Errors.Should().HaveCount(1);

        result.ShouldHaveValidationErrorFor(x => x.Exclude);
    }
}
