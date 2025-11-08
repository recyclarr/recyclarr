using FluentValidation;
using Recyclarr.Common.FluentValidation;
using Recyclarr.Settings.Models;

namespace Recyclarr.Settings;

public class RecyclarrSettingsValidator : AbstractValidator<RecyclarrSettings>
{
    public RecyclarrSettingsValidator()
    {
        RuleFor(x => x.Notifications).SetValidator(new NotificationSettingsValidator());
        RuleFor(x => x.ResourceProviders).SetValidator(new ResourceProviderSettingsValidator());
    }
}

public class ResourceProviderSettingsValidator : AbstractValidator<ResourceProviderSettings>
{
    public ResourceProviderSettingsValidator()
    {
        RuleForEach(x => x.Providers).SetValidator(new ResourceProviderValidator());

        // Validate globally unique names
        RuleFor(x => x.Providers)
            .Must(providers =>
            {
                var names = providers.Select(p => p.Name).ToList();
                var distinctNames = names.Distinct().ToList();
                return names.Count == distinctNames.Count;
            })
            .WithMessage("Provider names must be globally unique");

        // Validate only one replace_default per type
        RuleFor(x => x.Providers)
            .Must(
                (settings, providers, context) =>
                {
                    var duplicateTypes = providers
                        .Where(p => p.ReplaceDefault)
                        .GroupBy(p => p.Type)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key)
                        .ToList();

                    if (duplicateTypes.Any())
                    {
                        context.MessageFormatter.AppendArgument(
                            "DuplicateTypes",
                            string.Join(", ", duplicateTypes)
                        );
                        return false;
                    }

                    return true;
                }
            )
            .WithMessage("Multiple providers have replace_default for types: {DuplicateTypes}");
    }
}

public class ResourceProviderValidator : AbstractValidator<ResourceProvider>
{
    public ResourceProviderValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Provider name is required")
            .Matches("^[a-zA-Z0-9_-]+$")
            .WithMessage(
                "Provider name must contain only letters, numbers, hyphens, and underscores"
            );

        RuleFor(x => x.Type)
            .NotEmpty()
            .WithMessage("Provider type is required")
            .Must(type => type is "trash-guides" or "config-templates" or "custom-formats")
            .WithMessage(
                "Provider type must be one of: trash-guides, config-templates, custom-formats"
            );

        // Git provider validations
        When(
            x => x is GitResourceProvider,
            () =>
            {
                RuleFor(x => (x as GitResourceProvider)!.CloneUrl)
                    .Must(uri => uri?.Scheme != "about")
                    .WithMessage("Provider clone URL is required");
            }
        );

        // Local provider validations
        When(
            x => x is LocalResourceProvider,
            () =>
            {
                RuleFor(x => (x as LocalResourceProvider)!.Path)
                    .NotEmpty()
                    .WithMessage("Provider path is required");

                // Service required for custom-formats type
                When(
                    p => p.Type == "custom-formats",
                    () =>
                    {
                        RuleFor(x => (x as LocalResourceProvider)!.Service)
                            .NotEmpty()
                            .WithMessage("Service is required for custom-formats providers")
                            .Must(service => service is "radarr" or "sonarr")
                            .WithMessage("Service must be either 'radarr' or 'sonarr'");
                    }
                );
            }
        );

        // Git provider with custom-formats type must have service on associated local provider
        // Note: Git custom-formats providers don't need service property (determined by repo structure)
    }
}

public class NotificationSettingsValidator : AbstractValidator<NotificationSettings>
{
    public NotificationSettingsValidator()
    {
        RuleFor(x => x.Apprise).SetNonNullableValidator(new AppriseNotificationSettingsValidator());
    }
}

public class AppriseNotificationSettingsValidator : AbstractValidator<AppriseNotificationSettings>
{
    public AppriseNotificationSettingsValidator()
    {
        RuleFor(x => x.Mode).NotNull().WithMessage("`mode` is required for apprise notifications");

        RuleFor(x => x.BaseUrl)
            .NotEmpty()
            .WithMessage("`base_url` is required for apprise notifications");

        RuleFor(x => x.Urls)
            .NotEmpty()
            .When(x => x.Mode == AppriseMode.Stateless)
            .WithMessage(
                "`urls` is required when `mode` is set to `stateless` for apprise notifications"
            );

        RuleFor(x => x.Key)
            .NotEmpty()
            .When(x => x.Mode == AppriseMode.Stateful)
            .WithMessage(
                "`key` is required when `mode` is set to `stateful` for apprise notifications"
            );
    }
}
