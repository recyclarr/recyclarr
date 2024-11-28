using FluentValidation;
using Recyclarr.Common.Extensions;
using Recyclarr.Common.FluentValidation;

namespace Recyclarr.Config.Parsing;

public class ServiceConfigYamlValidator : AbstractValidator<ServiceConfigYaml>
{
    public ServiceConfigYamlValidator()
    {
        RuleSet(
            YamlValidatorRuleSets.RootConfig,
            () =>
            {
                RuleFor(x => x.BaseUrl)
                    .Cascade(CascadeMode.Stop)
                    .NotEmpty()
                    .Must(x => x!.StartsWith("http", StringComparison.Ordinal))
                    .WithMessage("{PropertyName} must start with 'http' or 'https'")
                    .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
                    .WithMessage("{PropertyName} must be a valid URL")
                    .WithName("base_url");

                RuleFor(x => x.ApiKey).NotEmpty().WithName("api_key");
            }
        );

        RuleFor(x => x.CustomFormats)
            .NotEmpty()
            .When(x => x.CustomFormats is not null)
            .ForEach(x => x.SetValidator(new CustomFormatConfigYamlValidator()))
            .WithName("custom_formats");

        RuleFor(x => x.QualityDefinition)
            .SetNonNullableValidator(new QualitySizeConfigYamlValidator());

        RuleFor(x => x.QualityProfiles)
            .NotEmpty()
            .When(x => x.QualityProfiles != null)
            .WithName("quality_profiles")
            .ForEach(x => x.SetValidator(new QualityProfileConfigYamlValidator()));
    }
}

public class CustomFormatConfigYamlValidator : AbstractValidator<CustomFormatConfigYaml>
{
    public CustomFormatConfigYamlValidator()
    {
        RuleForEach(x => x.AssignScoresTo).SetValidator(new QualityScoreConfigYamlValidator());
    }
}

public class QualityScoreConfigYamlValidator : AbstractValidator<QualityScoreConfigYaml>
{
    public QualityScoreConfigYamlValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("'name' is required for elements under 'quality_profiles'");
    }
}

public class QualitySizeConfigYamlValidator : AbstractValidator<QualitySizeConfigYaml>
{
    public QualitySizeConfigYamlValidator()
    {
        RuleFor(x => x.Type).NotEmpty().WithMessage("'type' is required for 'quality_definition'");

        RuleFor(x => x.PreferredRatio)
            .InclusiveBetween(0, 1)
            .When(x => x.PreferredRatio is not null)
            .WithName("preferred_ratio");
    }
}

public class QualityProfileFormatUpgradeYamlValidator
    : AbstractValidator<QualityProfileFormatUpgradeYaml>
{
    public QualityProfileFormatUpgradeYamlValidator(QualityProfileConfigYaml config)
    {
        RuleFor(x => x.Allowed)
            .NotNull()
            .WithMessage(
                $"For profile {config.Name}, 'allowed' under 'upgrade' is required. "
                    + $"If you don't want Recyclarr to manage upgrades, delete the whole 'upgrade' block."
            );

        RuleFor(x => x.UntilQuality)
            .NotNull()
            .When(x => x.Allowed is true && config.Qualities is not null)
            .WithMessage(
                $"For profile {config.Name}, 'until_quality' is required when 'allowed' is set to 'true' and "
                    + $"an explicit 'qualities' list is provided."
            );
    }
}

public class ResetUnmatchedScoresConfigYamlValidator
    : AbstractValidator<ResetUnmatchedScoresConfigYaml>
{
    public ResetUnmatchedScoresConfigYamlValidator()
    {
        RuleFor(x => x.Enabled)
            .NotNull()
            .WithMessage("Under `reset_unmatched_scores`, the `enabled` property is required.");
    }
}

public class QualityProfileConfigYamlValidator : AbstractValidator<QualityProfileConfigYaml>
{
    public QualityProfileConfigYamlValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(x =>
                $"For profile {x.Name}, 'name' is required for root-level 'quality_profiles' elements"
            );

        RuleFor(x => x.Upgrade)
            .SetNonNullableValidator(x => new QualityProfileFormatUpgradeYamlValidator(x));

        RuleFor(x => x.ResetUnmatchedScores)
            .SetNonNullableValidator(new ResetUnmatchedScoresConfigYamlValidator());

        RuleFor(x => x.Qualities)
            .Custom(ValidateHaveNoDuplicates!)
            .Custom(ValidateGroupQualityCount!)
            .Must(x => x!.Any(y => y.Enabled is true or null))
            .WithMessage(x =>
                $"For profile {x.Name}, at least one explicitly listed quality under 'qualities' must be enabled."
            )
            .When(x => x is { Qualities.Count: > 0 });

        RuleFor(x => x.Qualities)
            .Must(
                (o, x) =>
                    !x!
                        .Where(y => y is { Enabled: false, Name: not null })
                        .Select(y => y.Name!)
                        .Contains(
                            o.Upgrade!.UntilQuality,
                            StringComparer.InvariantCultureIgnoreCase
                        )
            )
            .WithMessage(o =>
                $"For profile {o.Name}, 'until_quality' must not refer to explicitly disabled qualities"
            )
            .Must(
                (o, x) =>
                    x!
                        .Select(y => y.Name)
                        .Contains(
                            o.Upgrade!.UntilQuality,
                            StringComparer.InvariantCultureIgnoreCase
                        )
            )
            .WithMessage(o =>
                $"For profile {o.Name}, 'qualities' must contain the quality mentioned in 'until_quality', "
                + $"which is '{o.Upgrade!.UntilQuality}'"
            )
            .When(x => x is { Upgrade.Allowed: not false, Qualities.Count: > 0 });
    }

    private static void ValidateGroupQualityCount(
        IReadOnlyCollection<QualityProfileQualityConfigYaml> qualities,
        ValidationContext<QualityProfileConfigYaml> context
    )
    {
        // Find groups with less than 2 items
        var invalidCount = qualities.Count(x => x.Qualities?.Count < 2);

        if (invalidCount != 0)
        {
            var profile = context.InstanceToValidate;
            context.AddFailure(
                $"For profile {profile.Name}, 'qualities' contains {invalidCount} groups with less than 2 qualities"
            );
        }
    }

    private static void ValidateHaveNoDuplicates(
        IReadOnlyCollection<QualityProfileQualityConfigYaml> qualities,
        ValidationContext<QualityProfileConfigYaml> context
    )
    {
        // Check for quality duplicates between non-groups and groups
        var qualityDupes = qualities
            .Where(x => x.Qualities is null)
            .Select(x => x.Name)
            .Concat(qualities.Where(x => x.Qualities is not null).SelectMany(x => x.Qualities!))
            .GroupBy(x => x)
            .Select(x => x.Skip(1).FirstOrDefault())
            .NotNull();

        foreach (var dupe in qualityDupes)
        {
            var x = context.InstanceToValidate;
            context.AddFailure(
                $"For profile {x.Name}, 'qualities' contains duplicates for quality '{dupe}'"
            );
        }

        // Check for quality duplicates between non-groups and groups
        var groupDupes = qualities
            .Where(x => x.Qualities is not null)
            .Select(x => x.Name)
            .GroupBy(x => x)
            .Select(x => x.Skip(1).FirstOrDefault())
            .NotNull();

        foreach (var dupe in groupDupes)
        {
            var x = context.InstanceToValidate;
            context.AddFailure(
                $"For profile {x.Name}, 'qualities' contains duplicates for quality group '{dupe}'"
            );
        }
    }
}

public class RadarrConfigYamlValidator : CustomValidator<RadarrConfigYaml>
{
    public RadarrConfigYamlValidator()
    {
        Include(new ServiceConfigYamlValidator());
    }
}

public class SonarrConfigYamlValidator : CustomValidator<SonarrConfigYaml>
{
    public SonarrConfigYamlValidator()
    {
        Include(new ServiceConfigYamlValidator());
    }
}

public class RootConfigYamlValidator : CustomValidator<RootConfigYaml>
{
    public RootConfigYamlValidator()
    {
        RuleForEach(x => x.RadarrValues).SetValidator(new RadarrConfigYamlValidator());
        RuleForEach(x => x.SonarrValues).SetValidator(new SonarrConfigYamlValidator());
    }
}
