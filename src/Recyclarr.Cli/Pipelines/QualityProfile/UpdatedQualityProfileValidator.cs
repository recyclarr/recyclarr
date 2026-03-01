using FluentValidation;
using Recyclarr.Common.Extensions;

namespace Recyclarr.Cli.Pipelines.QualityProfile;

internal class UpdatedQualityProfileValidator : AbstractValidator<UpdatedQualityProfile>
{
    public UpdatedQualityProfileValidator()
    {
        RuleFor(x => x.EffectiveMinFormatScore).Custom(ValidateMinScoreSatisfied);

        RuleFor(x => x.ProfileConfig.Config.UpgradeUntilQuality)
            .Custom(ValidateCutoff!)
            .When(x => x.ProfileConfig.Config.UpgradeUntilQuality is not null);

        // Qualities are consolidated in Plan phase (from config or guide resource)
        // New profiles (those with no service ID) require qualities to be specified
        RuleFor(x => x.ProfileConfig.Config.Qualities)
            .NotEmpty()
            .When(x => x.Profile.Id is null)
            .WithMessage("`qualities` is required when creating profiles for the first time");
    }

    private static void ValidateMinScoreSatisfied(
        int? minScore,
        ValidationContext<UpdatedQualityProfile> context
    )
    {
        if (minScore is not > 0)
        {
            return;
        }

        var scores = context.InstanceToValidate.UpdatedScores.Select(x => x.NewScore).ToList();
        var totalPositiveScores = scores.Where(x => x > 0).Sum();
        var maxScore = scores.Count > 0 ? scores.Max() : 0;

        // Match Sonarr's validation: fail only if both sum AND max are below minimum
        if (totalPositiveScores < minScore && maxScore < minScore)
        {
            context.AddFailure(
                $"Minimum Custom Format Score of {minScore} can never be satisfied because the total of all "
                    + $"positive scores is {totalPositiveScores} and no single score meets the minimum"
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
                : profile.Profile.Items;

        if (items.FindCutoff(untilQuality) is null)
        {
            context.AddFailure(
                "'until_quality' must refer to an existing and enabled quality or group"
            );
        }
    }
}
