using Common.FluentValidation;
using FluentValidation;
using JetBrains.Annotations;
using TrashLib.Config.Services;

namespace TrashLib.Services.Radarr.Config;

[UsedImplicitly]
internal class RadarrConfigurationValidator : AbstractValidator<RadarrConfiguration>
{
    public RadarrConfigurationValidator(
        IValidator<ServiceConfiguration> serviceConfigValidator,
        IValidator<QualityDefinitionConfig> qualityDefinitionConfigValidator)
    {
        Include(serviceConfigValidator);
        RuleFor(x => x.QualityDefinition).SetNonNullableValidator(qualityDefinitionConfigValidator);
    }
}

[UsedImplicitly]
internal class QualityDefinitionConfigValidator : AbstractValidator<QualityDefinitionConfig>
{
    public QualityDefinitionConfigValidator(IRadarrValidationMessages messages)
    {
        RuleFor(x => x.Type).NotEmpty().WithMessage(messages.QualityDefinitionType);
    }
}
