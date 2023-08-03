using FluentValidation;
using Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;

namespace Recyclarr.Cli.Pipelines.QualityProfile;

public class UpdatedQualityProfileValidator : AbstractValidator<UpdatedQualityProfile>
{
    public UpdatedQualityProfileValidator()
    {
        RuleFor(x => x.ProfileConfig.Profile.MinFormatScore).Custom((minScore, context) =>
        {
            var scores = context.InstanceToValidate.UpdatedScores;
            var totalScores = scores.Select(x => x.NewScore).Where(x => x > 0).Sum();
            if (totalScores < minScore)
            {
                context.AddFailure(
                    $"Minimum Custom Format Score of {minScore} can never be satisfied because the total of all " +
                    $"positive scores is {totalScores}");
            }
        });

        RuleFor(x => x.ProfileConfig.Profile.UpgradeUntilQuality)
            .Must((o, x) => o.ProfileDto.Items.FindCutoff(x) is not null)
            .When(x => x.ProfileConfig.Profile is {UpgradeUntilQuality: not null, Qualities.Count: 0})
            .WithMessage("'until_quality' must refer to an existing and enabled quality or group");

        RuleFor(x => x.ProfileConfig.Profile.UpgradeUntilQuality)
            .Must((o, x)
                => !o.UpdatedQualities.InvalidQualityNames.Contains(x, StringComparer.InvariantCultureIgnoreCase))
            .WithMessage((_, x) => $"`until_quality` references invalid quality '{x}'");

        RuleFor(x => x.ProfileConfig.Profile.Qualities)
            .NotEmpty()
            .When(x => x.UpdateReason == QualityProfileUpdateReason.New)
            .WithMessage("`qualities` is required when creating profiles for the first time");
    }
}
