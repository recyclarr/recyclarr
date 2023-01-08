using FluentValidation;
using JetBrains.Annotations;
using Recyclarr.Common.Extensions;
using Recyclarr.Common.FluentValidation;

namespace Recyclarr.TrashLib.Services.Sonarr.Config;

[UsedImplicitly]
public class SonarrConfigurationValidator : AbstractValidator<SonarrConfiguration>
{
    public SonarrConfigurationValidator()
    {
        RuleForEach(x => x.ReleaseProfiles)
            .Empty()
            .When(x => x.CustomFormats.NotEmpty())
            .WithMessage("`custom_formats` and `release_profiles` may not be used together");

        RuleForEach(x => x.ReleaseProfiles).SetValidator(new ReleaseProfileConfigValidator());
    }
}

[UsedImplicitly]
internal class ReleaseProfileConfigValidator : AbstractValidator<ReleaseProfileConfig>
{
    public ReleaseProfileConfigValidator()
    {
        RuleFor(x => x.TrashIds).NotEmpty().WithMessage("'trash_ids' is required for 'release_profiles' elements");
        RuleFor(x => x.Filter).SetNonNullableValidator(new SonarrProfileFilterConfigValidator());
    }
}

[UsedImplicitly]
internal class SonarrProfileFilterConfigValidator : AbstractValidator<SonarrProfileFilterConfig>
{
    public SonarrProfileFilterConfigValidator()
    {
        // Include & Exclude may not be used together
        RuleFor(x => x.Include).Empty().When(x => x.Exclude.Any())
            .WithMessage("`include` and `exclude` may not be used together.");
    }
}
