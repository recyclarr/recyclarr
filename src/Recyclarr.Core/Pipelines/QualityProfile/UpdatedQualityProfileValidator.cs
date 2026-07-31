using FluentValidation;
using FluentValidation.Results;
using Recyclarr.Common.Extensions;

namespace Recyclarr.Pipelines.QualityProfile;

internal enum QualityProfileValidationConstraint
{
    MinimumScoreUnsatisfied,
    InvalidCutoff,
    UnavailableCutoff,
    QualitiesRequired,
}

internal class UpdatedQualityProfileValidator : AbstractValidator<UpdatedQualityProfile>
{
    public UpdatedQualityProfileValidator()
    {
        RuleFor(x => x.EffectiveMinFormatScore).Custom(ValidateMinScoreSatisfied);

        RuleFor(x => x.ProfileConfig.Config.UpgradeUntilQuality)
            .Custom(ValidateInvalidCutoff!)
            .When(x => x.ProfileConfig.Config.UpgradeUntilQuality is not null);

        RuleFor(x => x.ProfileConfig.Config.UpgradeUntilQuality)
            .Custom(ValidateAvailableCutoff!)
            .When(x => x.ProfileConfig.Config.UpgradeUntilQuality is not null);

        // Qualities are consolidated in Plan phase (from config or guide resource)
        // New profiles (those with no service ID) require qualities to be specified
        RuleFor(x => x.ProfileConfig.Config.Qualities)
            .NotEmpty()
            .When(x => x.Profile.Id is null)
            .WithMessage("`qualities` is required when creating profiles for the first time")
            .WithErrorCode(nameof(QualityProfileValidationConstraint.QualitiesRequired));
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
            AddFailure(
                context,
                QualityProfileValidationConstraint.MinimumScoreUnsatisfied,
                $"Minimum Custom Format Score of {minScore} can never be satisfied because the total "
                    + $"of all positive scores is {totalPositiveScores} and no single score meets the minimum"
            );
        }
    }

    private static void ValidateInvalidCutoff(
        string untilQuality,
        ValidationContext<UpdatedQualityProfile> context
    )
    {
        var profile = context.InstanceToValidate;

        if (profile.UpdatedQualities.InvalidQualityNames.Any(x => x.EqualsIgnoreCase(untilQuality)))
        {
            AddFailure(
                context,
                QualityProfileValidationConstraint.InvalidCutoff,
                $"`until_quality` references invalid quality '{untilQuality}'"
            );
        }
    }

    private static void ValidateAvailableCutoff(
        string untilQuality,
        ValidationContext<UpdatedQualityProfile> context
    )
    {
        var profile = context.InstanceToValidate;
        if (profile.UpdatedQualities.InvalidQualityNames.Any(x => x.EqualsIgnoreCase(untilQuality)))
        {
            return;
        }

        var items =
            profile.UpdatedQualities.NumWantedItems > 0
                ? profile.UpdatedQualities.Items
                : profile.Profile.Items;

        if (items.FindCutoff(untilQuality) is null)
        {
            AddFailure(
                context,
                QualityProfileValidationConstraint.UnavailableCutoff,
                "'until_quality' must refer to an existing and enabled quality or group"
            );
        }
    }

    private static void AddFailure(
        ValidationContext<UpdatedQualityProfile> context,
        QualityProfileValidationConstraint constraint,
        string message
    )
    {
        context.AddFailure(
            new ValidationFailure(context.PropertyPath, message)
            {
                ErrorCode = constraint.ToString(),
            }
        );
    }
}
