using FluentValidation;
using Recyclarr.Cli.Pipelines.QualityProfile.Models;
using Recyclarr.Common.Extensions;

namespace Recyclarr.Cli.Pipelines.QualityProfile;

internal class UpdatedQualityProfileValidator : AbstractValidator<UpdatedQualityProfile>
{
    public UpdatedQualityProfileValidator()
    {
        RuleFor(x => x.ProfileConfig.Profile.MinFormatScore).Custom(ValidateMinScoreSatisfied);

        RuleFor(x => x.ProfileConfig.Profile.UpgradeUntilQuality)
            .Custom(ValidateCutoff!)
            .When(x => x.ProfileConfig.Profile.UpgradeUntilQuality is not null);

        RuleFor(x => x.ProfileConfig.Profile.Qualities)
            .NotEmpty()
            .When(x => x.UpdateReason == QualityProfileUpdateReason.New)
            .WithMessage("`qualities` is required when creating profiles for the first time");
    }

    private static void ValidateMinScoreSatisfied(
        int? minScore,
        ValidationContext<UpdatedQualityProfile> context
    )
    {
        var scores = context.InstanceToValidate.UpdatedScores;
        var totalScores = scores.Select(x => x.NewScore).Where(x => x > 0).Sum();
        if (totalScores < minScore)
        {
            context.AddFailure(
                $"Minimum Custom Format Score of {minScore} can never be satisfied because the total of all "
                    + $"positive scores is {totalScores}"
            );
        }
    }

    private static void ValidateCutoff(
        string untilQuality,
        ValidationContext<UpdatedQualityProfile> context
    )
    {
        var profile = context.InstanceToValidate;

        if (profile.UpdatedQualities.InvalidQualityNames.Any(x => x.EqualsIgnoreCase(untilQuality)))
        {
            context.AddFailure($"`until_quality` references invalid quality '{untilQuality}'");
            return;
        }

        var items =
            profile.UpdatedQualities.NumWantedItems > 0
                ? profile.UpdatedQualities.Items
                : profile.ProfileDto.Items;

        if (items.FindCutoff(untilQuality) is null)
        {
            context.AddFailure(
                "'until_quality' must refer to an existing and enabled quality or group"
            );
        }
    }
}
