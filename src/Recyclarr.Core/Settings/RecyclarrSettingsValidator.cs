using FluentValidation;
using Recyclarr.Common.FluentValidation;
using Recyclarr.Settings.Models;

namespace Recyclarr.Settings;

public class RecyclarrSettingsValidator : AbstractValidator<RecyclarrSettings>
{
    public RecyclarrSettingsValidator()
    {
        RuleFor(x => x.Notifications).SetValidator(new NotificationSettingsValidator());
        RuleForEach(x => x.ResourceProviders)
            .SetValidator(new ResourceProviderValidator())
            .SetInheritanceValidator(v =>
            {
                v.Add(new GitResourceProviderValidator());
                v.Add(new LocalResourceProviderValidator());
            });

        // Validate globally unique names
        RuleFor(x => x.ResourceProviders)
            .Must(providers =>
            {
                var names = providers.Select(p => p.Name).ToList();
                var distinctNames = names.Distinct().ToList();
                return names.Count == distinctNames.Count;
            })
            .WithMessage("Provider names must be globally unique");

        // Validate only one replace_default per type
        RuleFor(x => x.ResourceProviders)
            .Must(
                (_, providers, context) =>
                {
                    var duplicateTypes = providers
                        .Where(p => p.ReplaceDefault)
                        .GroupBy(p => p.Type)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key)
                        .ToList();

                    if (duplicateTypes.Count <= 0)
                    {
                        return true;
                    }

                    context.MessageFormatter.AppendArgument(
                        "DuplicateTypes",
                        string.Join(", ", duplicateTypes)
                    );

                    return false;
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

        // Service required for custom-formats, not allowed otherwise
        When(
                x => x.Type == "custom-formats",
                () =>
                {
                    RuleFor(x => x.Service)
                        .NotEmpty()
                        .WithMessage("Service is required for custom-formats providers")
                        .Must(service => service is "radarr" or "sonarr")
                        .WithMessage("Service must be either 'radarr' or 'sonarr'");
                }
            )
            .Otherwise(() =>
            {
                RuleFor(x => x.Service)
                    .Empty()
                    .WithMessage("Service field is only allowed for custom-formats providers");
            });
    }
}

public class GitResourceProviderValidator : AbstractValidator<GitResourceProvider>
{
    public GitResourceProviderValidator()
    {
        RuleFor(x => x.CloneUrl)
            .NotNull()
            .Must(uri => uri.IsAbsoluteUri && uri.Scheme is "http" or "https")
            .WithMessage("Provider clone URL must be a valid HTTP/HTTPS URL");
    }
}

public class LocalResourceProviderValidator : AbstractValidator<LocalResourceProvider>
{
    public LocalResourceProviderValidator()
    {
        RuleFor(x => x.Path).NotEmpty().WithMessage("Provider path is required");
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
            .Must(uri => uri.IsAbsoluteUri && uri.Scheme is "http" or "https")
            .WithMessage("`base_url` must be a valid HTTP/HTTPS URL");

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
