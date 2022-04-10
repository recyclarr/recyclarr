using FluentValidation;
using JetBrains.Annotations;

namespace TrashLib.Sonarr.Config;

[UsedImplicitly]
internal class SonarrConfigurationValidator : AbstractValidator<SonarrConfiguration>
{
    public SonarrConfigurationValidator(
        ISonarrValidationMessages messages,
        IValidator<ReleaseProfileConfig> releaseProfileConfigValidator)
    {
        RuleFor(x => x.BaseUrl).NotEmpty().WithMessage(messages.BaseUrl);
        RuleFor(x => x.ApiKey).NotEmpty().WithMessage(messages.ApiKey);
        RuleForEach(x => x.ReleaseProfiles).SetValidator(releaseProfileConfigValidator);
    }
}

[UsedImplicitly]
internal class ReleaseProfileConfigValidator : AbstractValidator<ReleaseProfileConfig>
{
    public ReleaseProfileConfigValidator(ISonarrValidationMessages messages)
    {
        RuleFor(x => x.TrashIds).NotEmpty().WithMessage(messages.ReleaseProfileTrashIds);
    }
}
