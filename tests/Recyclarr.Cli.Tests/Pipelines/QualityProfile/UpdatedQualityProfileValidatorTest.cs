using FluentValidation.TestHelper;
using Recyclarr.Cli.Pipelines.QualityProfile;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Tests.Pipelines.QualityProfile;

internal sealed class UpdatedQualityProfileValidatorTest
{
    // Sum of positive scores is 400: 100 + 200 + 100 = 400, max single score is 200
    // Fails if BOTH sum < min AND max < min
    [Test]
    [Arguments(199, true)] // sum(400) >= min, max(200) >= min -> pass
    [Arguments(200, true)] // sum(400) >= min, max(200) >= min -> pass
    [Arguments(201, true)] // sum(400) >= min, max(200) < min -> pass (sum satisfies)
    [Arguments(400, true)] // sum(400) >= min, max(200) < min -> pass (sum satisfies)
    [Arguments(401, false)] // sum(400) < min, max(200) < min -> fail
    [Arguments(500, false)] // sum(400) < min, max(200) < min -> fail
    public void Min_score_validation_considers_both_sum_and_max(int minScore, bool expectSatisfied)
    {
        var profileConfig = new QualityProfileConfig { MinFormatScore = minScore };

        var updatedProfile = new UpdatedQualityProfile
        {
            UpdatedScores =
            [
                NewQp.UpdatedScore("foo1", 0, 100, FormatScoreUpdateReason.New),
                NewQp.UpdatedScore("foo2", 0, -100, FormatScoreUpdateReason.Updated),
                NewQp.UpdatedScore("foo3", 0, 200, FormatScoreUpdateReason.NoChange),
                NewQp.UpdatedScore("foo4", 0, 100, FormatScoreUpdateReason.Reset),
            ],
            ProfileDto = new QualityProfileDto { Id = 1, Name = "ProfileName" },
            ProfileConfig = NewPlan.Qp(profileConfig),
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

            result
                .ShouldHaveValidationErrorFor(x => x.ProfileConfig.Config.MinFormatScore)
                .WithErrorMessage(
                    $"Minimum Custom Format Score of {minScore} can never be satisfied because the total of all "
                        + $"positive scores is {expectedTotalScore} and no single score meets the minimum"
                );
        }
    }

    [Test]
    [Arguments(null)]
    [Arguments(0)]
    [Arguments(-10)]
    public void Min_score_skipped_when_null_or_non_positive(int? minScore)
    {
        var profileConfig = new QualityProfileConfig { MinFormatScore = minScore };

        var updatedProfile = new UpdatedQualityProfile
        {
            UpdatedScores = [], // Empty scores would fail validation otherwise
            ProfileDto = new QualityProfileDto { Id = 1, Name = "ProfileName" },
            ProfileConfig = NewPlan.Qp(profileConfig),
        };

        var validator = new UpdatedQualityProfileValidator();
        var result = validator.TestValidate(updatedProfile);

        // Should not fail on MinFormatScore (may fail on other rules)
        result.ShouldNotHaveValidationErrorFor(x => x.ProfileConfig.Config.MinFormatScore);
    }

    [Test]
    public void Min_score_with_empty_scores_and_positive_min_fails()
    {
        var profileConfig = new QualityProfileConfig { MinFormatScore = 100 };

        var updatedProfile = new UpdatedQualityProfile
        {
            UpdatedScores = [],
            ProfileDto = new QualityProfileDto { Id = 1, Name = "ProfileName" },
            ProfileConfig = NewPlan.Qp(profileConfig),
        };

        var validator = new UpdatedQualityProfileValidator();
        var result = validator.TestValidate(updatedProfile);

        result
            .ShouldHaveValidationErrorFor(x => x.ProfileConfig.Config.MinFormatScore)
            .WithErrorMessage(
                "Minimum Custom Format Score of 100 can never be satisfied because the total of all "
                    + "positive scores is 0 and no single score meets the minimum"
            );
    }

    [Test]
    public void Until_quality_references_invalid_quality()
    {
        var profileConfig = new QualityProfileConfig { UpgradeUntilQuality = "foo1" };

        var updatedProfile = new UpdatedQualityProfile
        {
            UpdatedQualities = new UpdatedQualities { InvalidQualityNames = ["foo1"] },
            ProfileDto = new QualityProfileDto(),
            ProfileConfig = NewPlan.Qp(profileConfig),
        };

        var validator = new UpdatedQualityProfileValidator();
        var result = validator.TestValidate(updatedProfile);

        result
            .ShouldHaveValidationErrorFor(x => x.ProfileConfig.Config.UpgradeUntilQuality)
            .WithErrorMessage("`until_quality` references invalid quality 'foo1'");
    }

    [Test]
    public void Qualities_required_for_new_profiles()
    {
        var profileConfig = new QualityProfileConfig();

        var updatedProfile = new UpdatedQualityProfile
        {
            ProfileDto = new QualityProfileDto(),
            ProfileConfig = NewPlan.Qp(profileConfig),
        };

        var validator = new UpdatedQualityProfileValidator();
        var result = validator.TestValidate(updatedProfile);

        result
            .ShouldHaveValidationErrorFor(x => x.ProfileConfig.Config.Qualities)
            .WithErrorMessage("`qualities` is required when creating profiles for the first time");
    }

    [Test]
    public void Cutoff_quality_must_be_enabled_without_qualities_list()
    {
        var profileConfig = new QualityProfileConfig { UpgradeUntilQuality = "disabled_quality" };

        var updatedProfile = new UpdatedQualityProfile
        {
            ProfileDto = new QualityProfileDto
            {
                Items = [NewQp.QualityDto(1, "disabled_quality", false)],
            },
            ProfileConfig = NewPlan.Qp(profileConfig),
        };

        var validator = new UpdatedQualityProfileValidator();
        var result = validator.TestValidate(updatedProfile);

        result
            .ShouldHaveValidationErrorFor(x => x.ProfileConfig.Config.UpgradeUntilQuality)
            .WithErrorMessage(
                "'until_quality' must refer to an existing and enabled quality or group"
            );
    }
}
