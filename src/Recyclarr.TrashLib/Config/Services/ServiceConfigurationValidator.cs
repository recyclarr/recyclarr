using FluentValidation;
using JetBrains.Annotations;
using Recyclarr.TrashLib.Services.Radarr.Config;

namespace Recyclarr.TrashLib.Config.Services;

[UsedImplicitly]
internal class ServiceConfigurationValidator : AbstractValidator<ServiceConfiguration>
{
    public ServiceConfigurationValidator(
        IServiceValidationMessages messages,
        IValidator<CustomFormatConfig> customFormatConfigValidator)
    {
        RuleFor(x => x.BaseUrl).NotEmpty().WithMessage(messages.BaseUrl);
        RuleFor(x => x.ApiKey).NotEmpty().WithMessage(messages.ApiKey);
        RuleForEach(x => x.CustomFormats).SetValidator(customFormatConfigValidator);
    }
}

[UsedImplicitly]
internal class CustomFormatConfigValidator : AbstractValidator<CustomFormatConfig>
{
    public CustomFormatConfigValidator(
        IServiceValidationMessages messages,
        IValidator<QualityProfileScoreConfig> qualityProfileScoreConfigValidator)
    {
        RuleFor(x => x.TrashIds).NotEmpty().WithMessage(messages.CustomFormatTrashIds);
        RuleForEach(x => x.QualityProfiles).SetValidator(qualityProfileScoreConfigValidator);
    }
}

[UsedImplicitly]
internal class QualityProfileScoreConfigValidator : AbstractValidator<QualityProfileScoreConfig>
{
    public QualityProfileScoreConfigValidator(IRadarrValidationMessages messages)
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(messages.QualityProfileName);
    }
}
