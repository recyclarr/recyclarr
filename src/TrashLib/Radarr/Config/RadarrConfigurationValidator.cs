using Common.Extensions;
using FluentValidation;
using JetBrains.Annotations;

namespace TrashLib.Radarr.Config
{
    [UsedImplicitly]
    internal class RadarrConfigurationValidator : AbstractValidator<RadarrConfig>
    {
        public RadarrConfigurationValidator(
            IRadarrValidationMessages messages,
            IValidator<QualityDefinitionConfig> qualityDefinitionConfigValidator,
            IValidator<CustomFormatConfig> customFormatConfigValidator)
        {
            // RuleFor(x => x.BaseUrl).NotEmpty().WithMessage(messages.BaseUrl);
            // RuleFor(x => x.ApiKey).NotEmpty().WithMessage(messages.ApiKey);
            // RuleFor(x => x.QualityDefinition).SetNonNullableValidator(qualityDefinitionConfigValidator);
            // RuleForEach(x => x.CustomFormats).SetValidator(customFormatConfigValidator);
        }
    }

    [UsedImplicitly]
    internal class CustomFormatConfigValidator : AbstractValidator<CustomFormatConfig>
    {
        public CustomFormatConfigValidator(
            IRadarrValidationMessages messages,
            IValidator<QualityProfileConfig> qualityProfileConfigValidator)
        {
            // RuleFor(x => x.Names).NotEmpty().When(x => x.TrashIds.Count == 0)
            //     .WithMessage(messages.CustomFormatNamesAndIds);
            // RuleForEach(x => x.QualityProfiles).SetValidator(qualityProfileConfigValidator);
        }
    }

    [UsedImplicitly]
    internal class QualityProfileConfigValidator : AbstractValidator<QualityProfileConfig>
    {
        public QualityProfileConfigValidator(IRadarrValidationMessages messages)
        {
            // RuleFor(x => x.Name).NotEmpty().WithMessage(messages.QualityProfileName);
        }
    }

    [UsedImplicitly]
    internal class QualityDefinitionConfigValidator : AbstractValidator<QualityDefinitionConfig>
    {
        public QualityDefinitionConfigValidator(IRadarrValidationMessages messages)
        {
            // RuleFor(x => x.Type).IsInEnum().WithMessage(messages.QualityDefinitionType);
        }
    }
}
