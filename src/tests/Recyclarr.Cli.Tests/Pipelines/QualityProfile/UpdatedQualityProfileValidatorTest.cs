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
            UpdateReason = QualityProfileUpdateReason.New
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
}
