using FluentValidation;
using JetBrains.Annotations;
using TrashLib.Config.Services;

namespace TrashLib.Services.Sonarr.Config;

[UsedImplicitly]
internal class SonarrConfigurationValidator : AbstractValidator<SonarrConfiguration>
{
    public SonarrConfigurationValidator(
        ISonarrValidationMessages messages,
        IValidator<ServiceConfiguration> serviceConfigValidator,
        IValidator<ReleaseProfileConfig> releaseProfileConfigValidator)
    {
        Include(serviceConfigValidator);
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
