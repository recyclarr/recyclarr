using FluentValidation;
using JetBrains.Annotations;
using Recyclarr.Common.FluentValidation;

namespace Recyclarr.TrashLib.Services.Sonarr.Config;

[UsedImplicitly]
public class SonarrConfigurationValidator : AbstractValidator<SonarrConfiguration>
{
    public SonarrConfigurationValidator(SonarrCapabilities capabilities)
    {
        RuleForEach(x => x.ReleaseProfiles).SetValidator(new ReleaseProfileConfigValidator());

        // Release profiles may not be used with Sonarr v4
        RuleFor(x => x)
            .Must(_ => capabilities.SupportsNamedReleaseProfiles)
            .WithMessage(
                $"Your Sonarr version {capabilities.Version} does not meet the minimum " +
                $"required version of {SonarrCapabilities.MinimumVersion}.");

        // Release profiles may not be used with Sonarr v4
        RuleFor(x => x.ReleaseProfiles).Empty()
            .When(_ => capabilities.SupportsCustomFormats)
            .WithMessage("Release profiles require Sonarr v3. " +
                "Please use `custom_formats` instead or use the right version of Sonarr.");

        // Custom formats may not be used with Sonarr v3
        RuleFor(x => x.CustomFormats).Empty()
            .When(_ => !capabilities.SupportsCustomFormats)
            .WithMessage("Custom formats require Sonarr v4 or greater. " +
                "Please use `release_profiles` instead or use the right version of Sonarr.");
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
