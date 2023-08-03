using FluentValidation.TestHelper;
using Recyclarr.Cli.Pipelines.QualityProfile;
using Recyclarr.Cli.Pipelines.QualityProfile.Api;
using Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;
using Recyclarr.Cli.TestLibrary;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.Cli.Tests.Pipelines.QualityProfile;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class UpdatedQualityProfileValidatorTest
{
    [TestCase(399, true)]
    [TestCase(400, true)]
    [TestCase(401, false)]
    public void Min_score_never_satisfied(int minScore, bool expectSatisfied)
    {
        var profileConfig = new QualityProfileConfig {MinFormatScore = minScore};

        var updatedProfile = new UpdatedQualityProfile
        {
            UpdatedScores = new[]
            {
                NewQp.UpdatedScore("foo1", 0, 100, FormatScoreUpdateReason.New),
                NewQp.UpdatedScore("foo2", 0, -100, FormatScoreUpdateReason.Updated),
                NewQp.UpdatedScore("foo3", 0, 200, FormatScoreUpdateReason.NoChange),
                NewQp.UpdatedScore("foo4", 0, 100, FormatScoreUpdateReason.Reset)
            },
            ProfileDto = new QualityProfileDto {Name = "ProfileName"},
            ProfileConfig = new ProcessedQualityProfileData(profileConfig),
            UpdateReason = QualityProfileUpdateReason.Changed
        };

        var validator = new UpdatedQualityProfileValidator();
        var result = validator.TestValidate(updatedProfile);

        if (expectSatisfied)
        {
            result.ShouldNotHaveAnyValidationErrors();
        }
        else
        {
            const int expectedTotalScore = 400;

            result.ShouldHaveValidationErrorFor(x => x.ProfileConfig.Profile.MinFormatScore)
                .WithErrorMessage(
                    $"Minimum Custom Format Score of {minScore} can never be satisfied because the total of all " +
                    $"positive scores is {expectedTotalScore}");
        }
    }

    [Test]
    public void Until_quality_references_invalid_quality()
    {
        var profileConfig = new QualityProfileConfig
        {
            UpgradeUntilQuality = "foo1"
        };

        var updatedProfile = new UpdatedQualityProfile
        {
            UpdatedQualities = new UpdatedQualities
            {
                InvalidQualityNames = new[] {"foo1"}
            },
            ProfileDto = new QualityProfileDto(),
            ProfileConfig = new ProcessedQualityProfileData(profileConfig),
            UpdateReason = QualityProfileUpdateReason.New
        };

        var validator = new UpdatedQualityProfileValidator();
        var result = validator.TestValidate(updatedProfile);

        result.ShouldHaveValidationErrorFor(x => x.ProfileConfig.Profile.UpgradeUntilQuality)
            .WithErrorMessage("`until_quality` references invalid quality 'foo1'");
    }

    [Test]
    public void Qualities_required_for_new_profiles()
    {
        var profileConfig = new QualityProfileConfig();

        var updatedProfile = new UpdatedQualityProfile
        {
            ProfileDto = new QualityProfileDto(),
            ProfileConfig = new ProcessedQualityProfileData(profileConfig),
            UpdateReason = QualityProfileUpdateReason.New
        };

        var validator = new UpdatedQualityProfileValidator();
        var result = validator.TestValidate(updatedProfile);

        result.ShouldHaveValidationErrorFor(x => x.ProfileConfig.Profile.Qualities)
            .WithErrorMessage("`qualities` is required when creating profiles for the first time");
    }

    [Test]
    public void Cutoff_quality_must_be_enabled_without_qualities_list()
    {
        var profileConfig = new QualityProfileConfig
        {
            UpgradeUntilQuality = "disabled_quality"
        };

        var updatedProfile = new UpdatedQualityProfile
        {
            ProfileDto = new QualityProfileDto
            {
                Items = new[]
                {
                    NewQp.QualityDto(1, "disabled_quality", false)
                }
            },
            ProfileConfig = new ProcessedQualityProfileData(profileConfig),
            UpdateReason = QualityProfileUpdateReason.New
        };

        var validator = new UpdatedQualityProfileValidator();
        var result = validator.TestValidate(updatedProfile);

        result.ShouldHaveValidationErrorFor(x => x.ProfileConfig.Profile.UpgradeUntilQuality)
            .WithErrorMessage("'until_quality' must refer to an existing and enabled quality or group");
    }
}
