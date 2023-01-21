using FluentValidation;
using JetBrains.Annotations;
using Recyclarr.Common.FluentValidation;
using Recyclarr.TrashLib.Services.Radarr.Config;
using Recyclarr.TrashLib.Services.Sonarr.Config;

namespace Recyclarr.TrashLib.Config.Services;

[UsedImplicitly]
internal class ServiceConfigurationValidator : AbstractValidator<ServiceConfiguration>
{
    public ServiceConfigurationValidator(
        IValidator<SonarrConfiguration> sonarrValidator,
        IValidator<RadarrConfiguration> radarrValidator)
    {
        RuleFor(x => x.InstanceName).NotEmpty();
        RuleFor(x => x.ServiceName).NotEmpty();
        RuleFor(x => x.LineNumber).NotEqual(0);
        RuleFor(x => x.BaseUrl).Must(x => x.Scheme is "http" or "https")
            .WithMessage("Property 'base_url' is required and must be a valid URL");
        RuleFor(x => x.ApiKey).NotEmpty().WithMessage("Property 'api_key' is required");
        RuleForEach(x => x.CustomFormats).SetValidator(new CustomFormatConfigValidator());
        RuleFor(x => x.QualityDefinition).SetNonNullableValidator(new QualityDefinitionConfigValidator());

        RuleFor(x => x).SetInheritanceValidator(x =>
        {
            x.Add(sonarrValidator);
            x.Add(radarrValidator);
        });
    }
}

[UsedImplicitly]
internal class CustomFormatConfigValidator : AbstractValidator<CustomFormatConfig>
{
    public CustomFormatConfigValidator()
    {
        RuleFor(x => x.TrashIds).NotEmpty()
            .WithMessage("'custom_formats' elements must contain at least one element under 'trash_ids'");
        RuleForEach(x => x.QualityProfiles).SetValidator(new QualityProfileScoreConfigValidator());
    }
}

[UsedImplicitly]
internal class QualityProfileScoreConfigValidator : AbstractValidator<QualityProfileScoreConfig>
{
    public QualityProfileScoreConfigValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("'name' is required for elements under 'quality_profiles'");
    }
}

[UsedImplicitly]
internal class QualityDefinitionConfigValidator : AbstractValidator<QualityDefinitionConfig>
{
    public QualityDefinitionConfigValidator()
    {
        RuleFor(x => x.Type).NotEmpty().WithMessage("'type' is required for 'quality_definition'");
    }
}
