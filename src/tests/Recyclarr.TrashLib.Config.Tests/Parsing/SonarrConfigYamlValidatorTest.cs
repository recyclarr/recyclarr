using FluentValidation.TestHelper;
using Recyclarr.TrashLib.Config.Parsing;

namespace Recyclarr.TrashLib.Config.Tests.Parsing;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class SonarrConfigYamlValidatorTest
{
    [Test]
    public void Validation_failure_when_rps_and_cfs_used_together()
    {
        var config = new SonarrConfigYaml
        {
            ReleaseProfiles = new[] {new ReleaseProfileConfigYaml()},
            CustomFormats = new[] {new CustomFormatConfigYaml()}
        };

        var validator = new SonarrConfigYamlValidator();
        var result = validator.TestValidate(config);

        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("`custom_formats` and `release_profiles` may not be used together");
    }

    [Test]
    public void Sonarr_release_profile_failures()
    {
        var config = new ReleaseProfileConfigYaml
        {
            TrashIds = Array.Empty<string>(),
            Filter = new ReleaseProfileFilterConfigYaml
            {
                Include = new[] {"include"},
                Exclude = new[] {"exclude"}
            }
        };

        var validator = new ReleaseProfileConfigYamlValidator();
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
        var config = new ReleaseProfileFilterConfigYaml
        {
            Include = Array.Empty<string>(),
            Exclude = new[] {"exclude"}
        };

        var validator = new ReleaseProfileFilterConfigYamlValidator();
        var result = validator.TestValidate(config);

        result.Errors.Should().HaveCount(1);

        result.ShouldHaveValidationErrorFor(x => x.Include);
    }

    [Test]
    public void Filter_exclude_can_not_be_empty()
    {
        var config = new ReleaseProfileFilterConfigYaml
        {
            Exclude = Array.Empty<string>(),
            Include = new[] {"exclude"}
        };

        var validator = new ReleaseProfileFilterConfigYamlValidator();
        var result = validator.TestValidate(config);

        result.Errors.Should().HaveCount(1);

        result.ShouldHaveValidationErrorFor(x => x.Exclude);
    }
}
