using FluentValidation;
using JetBrains.Annotations;
using Recyclarr.Common.Extensions;
using Recyclarr.Common.FluentValidation;

namespace Recyclarr.TrashLib.Config.Parsing;

[UsedImplicitly]
public class ServiceConfigYamlValidator : AbstractValidator<ServiceConfigYaml>
{
    public ServiceConfigYamlValidator()
    {
        RuleFor(x => x.BaseUrl).Cascade(CascadeMode.Stop)
            .NotEmpty().Must(x => x!.StartsWith("http"))
            .WithMessage("{PropertyName} must start with 'http' or 'https'")
            .WithName("base_url");

        // RuleFor(x => x.BaseUrl)
        //     .When(x => x.BaseUrl is {Length: > 0}, ApplyConditionTo.CurrentValidator)
        //     .WithMessage("{PropertyName} must start with 'http' or 'https'");

        RuleFor(x => x.ApiKey).NotEmpty().WithName("api_key");

        RuleFor(x => x.CustomFormats)
            .NotEmpty().When(x => x.CustomFormats is not null)
            .ForEach(x => x.SetValidator(new CustomFormatConfigYamlValidator()))
            .WithName("custom_formats");

        RuleFor(x => x.QualityDefinition)
            .SetNonNullableValidator(new QualitySizeConfigYamlValidator());

        RuleFor(x => x.QualityProfiles).NotEmpty()
            .When(x => x.QualityProfiles != null)
            .WithName("quality_profiles")
            .ForEach(x => x.SetValidator(new QualityProfileConfigYamlValidator()));
    }
}

[UsedImplicitly]
public class CustomFormatConfigYamlValidator : AbstractValidator<CustomFormatConfigYaml>
{
    public CustomFormatConfigYamlValidator()
    {
        RuleFor(x => x.TrashIds).NotEmpty()
            .When(x => x.TrashIds is not null)
            .WithName("trash_ids")
            .ForEach(x => x.Length(32).Matches(@"^[0-9a-fA-F]+$"));

        RuleForEach(x => x.QualityProfiles).NotEmpty()
            .When(x => x.QualityProfiles is not null)
            .WithName("quality_profiles")
            .SetValidator(new QualityScoreConfigYamlValidator());
    }
}

[UsedImplicitly]
public class QualityScoreConfigYamlValidator : AbstractValidator<QualityScoreConfigYaml>
{
    public QualityScoreConfigYamlValidator()
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage("'name' is required for elements under 'quality_profiles'");
    }
}

[UsedImplicitly]
public class QualitySizeConfigYamlValidator : AbstractValidator<QualitySizeConfigYaml>
{
    public QualitySizeConfigYamlValidator()
    {
        RuleFor(x => x.Type).NotEmpty()
            .WithMessage("'type' is required for 'quality_definition'");

        RuleFor(x => x.PreferredRatio).InclusiveBetween(0, 1)
            .When(x => x.PreferredRatio is not null)
            .WithName("preferred_ratio");
    }
}

[UsedImplicitly]
public class QualityProfileFormatUpgradeYamlValidator : AbstractValidator<QualityProfileFormatUpgradeYaml>
{
    public QualityProfileFormatUpgradeYamlValidator()
    {
        RuleFor(x => x.UntilQuality)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("'until_quality' is required when allowing profile upgrades");
    }
}

[UsedImplicitly]
public class QualityProfileConfigYamlValidator : AbstractValidator<QualityProfileConfigYaml>
{
    public QualityProfileConfigYamlValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(x => $"For profile {x.Name}, 'name' is required for root-level 'quality_profiles' elements");

        RuleFor(x => x.UpgradesAllowed)
            .SetNonNullableValidator(new QualityProfileFormatUpgradeYamlValidator());

        RuleFor(x => x.Qualities)
            .Cascade(CascadeMode.Stop)
            .Must((o, x) => !x!
                .Where(y => y.Qualities is not null)
                .SelectMany(y => y.Qualities!)
                .Contains(o.UpgradesAllowed!.UntilQuality, StringComparer.InvariantCultureIgnoreCase))
            .WithMessage(o =>
                $"For profile {o.Name}, 'until_quality' must not refer to qualities contained within groups")
            .Must((o, x) => !x!
                .Where(y => y is {Enabled: false, Name: not null})
                .Select(y => y.Name!)
                .Contains(o.UpgradesAllowed!.UntilQuality, StringComparer.InvariantCultureIgnoreCase))
            .WithMessage(o =>
                $"For profile {o.Name}, 'until_quality' must not refer to explicitly disabled qualities")
            .Must((o, x) => x!
                .Select(y => y.Name)
                .Contains(o.UpgradesAllowed!.UntilQuality, StringComparer.InvariantCultureIgnoreCase))
            .WithMessage(o =>
                $"For profile {o.Name}, 'qualities' must contain the quality mentioned in 'until_quality', " +
                $"which is '{o.UpgradesAllowed!.UntilQuality}'")
            .When(x => x is {UpgradesAllowed: not null, Qualities.Count: > 0});

        RuleFor(x => x.Qualities)
            .Custom(ValidateHaveNoDuplicates!)
            .Must(x => x!.Any(y => y.Enabled is true or null))
            .WithMessage(x =>
                $"For profile {x.Name}, at least one explicitly listed quality under 'qualities' must be enabled.")
            .When(x => x is {Qualities.Count: > 0});
    }

    private static void ValidateHaveNoDuplicates(
        IReadOnlyCollection<QualityProfileQualityConfigYaml> qualities,
        ValidationContext<QualityProfileConfigYaml> context)
    {
        var dupes = qualities
            .Select(x => x.Name)
            .Concat(qualities.Where(x => x.Qualities is not null).SelectMany(x => x.Qualities!))
            .NotNull()
            .GroupBy(x => x)
            .Select(x => x.Skip(1).FirstOrDefault())
            .NotNull();

        foreach (var dupe in dupes)
        {
            var x = context.InstanceToValidate;
            context.AddFailure(
                $"For profile {x.Name}, 'qualities' contains duplicates for quality '{dupe}'");
        }
    }
}

[UsedImplicitly]
public class RadarrConfigYamlValidator : CustomValidator<RadarrConfigYaml>
{
    public RadarrConfigYamlValidator()
    {
        Include(new ServiceConfigYamlValidator());
    }
}

[UsedImplicitly]
public class SonarrConfigYamlValidator : CustomValidator<SonarrConfigYaml>
{
    public SonarrConfigYamlValidator()
    {
        Include(new ServiceConfigYamlValidator());

        RuleFor(x => x)
            .Must(x => OnlyOneHasElements(x.ReleaseProfiles, x.CustomFormats))
            .WithMessage("`custom_formats` and `release_profiles` may not be used together");

        RuleForEach(x => x.ReleaseProfiles).SetValidator(new ReleaseProfileConfigYamlValidator());
    }
}

[UsedImplicitly]
public class ReleaseProfileConfigYamlValidator : CustomValidator<ReleaseProfileConfigYaml>
{
    public ReleaseProfileConfigYamlValidator()
    {
        RuleFor(x => x.TrashIds).NotEmpty()
            .WithMessage("'trash_ids' is required for 'release_profiles' elements");

        RuleFor(x => x.Filter)
            .SetNonNullableValidator(new ReleaseProfileFilterConfigYamlValidator());
    }
}

[UsedImplicitly]
public class ReleaseProfileFilterConfigYamlValidator : CustomValidator<ReleaseProfileFilterConfigYaml>
{
    public ReleaseProfileFilterConfigYamlValidator()
    {
        // Include & Exclude may not be used together
        RuleFor(x => x)
            .Must(x => OnlyOneHasElements(x.Include, x.Exclude))
            .WithMessage("'include' and 'exclude' may not be used together")
            .DependentRules(() =>
            {
                RuleFor(x => x.Include).NotEmpty()
                    .When(x => x.Include is not null)
                    .WithMessage("'include' under 'filter' must have at least 1 Trash ID");

                RuleFor(x => x.Exclude).NotEmpty()
                    .When(x => x.Exclude is not null)
                    .WithMessage("'exclude' under 'filter' must have at least 1 Trash ID");
            });
    }
}

[UsedImplicitly]
public class RootConfigYamlValidator : CustomValidator<RootConfigYaml>
{
    public RootConfigYamlValidator()
    {
        RuleForEach(x => x.RadarrValues).SetValidator(new RadarrConfigYamlValidator());
        RuleForEach(x => x.SonarrValues).SetValidator(new SonarrConfigYamlValidator());
    }
}
