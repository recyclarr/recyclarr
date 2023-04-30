using FluentValidation;
using JetBrains.Annotations;
using Recyclarr.Common.FluentValidation;

namespace Recyclarr.TrashLib.Config.Parsing;

[UsedImplicitly]
public class ServiceConfigYamlValidator : AbstractValidator<ServiceConfigYaml>
{
    public ServiceConfigYamlValidator()
    {
        RuleFor(x => x.BaseUrl).NotEmpty().NotNull()
            .WithMessage("'base_url' is required and must not be empty");

        RuleFor(x => x.BaseUrl).NotEmpty().Must(x => x is not null && x.StartsWith("http"))
            .WithMessage("'base_url' must start with 'http' or 'https'");

        RuleFor(x => x.ApiKey).NotEmpty()
            .WithMessage("'api_key' is required");

        RuleFor(x => x.CustomFormats).NotEmpty()
            .When(x => x.CustomFormats is not null)
            .WithName("custom_formats")
            .ForEach(x => x.SetValidator(new CustomFormatConfigYamlValidator()));

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
public class QualityProfileConfigYamlValidator : AbstractValidator<QualityProfileConfigYaml>
{
    public QualityProfileConfigYamlValidator()
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage("'name' is required for root-level 'quality_profiles' elements");
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
