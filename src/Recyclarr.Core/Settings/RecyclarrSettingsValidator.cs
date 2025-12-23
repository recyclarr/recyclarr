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
            .WithMessage(x =>
                $"Provider '{x.Name}': name must contain only letters, numbers, hyphens, and underscores"
            );

        RuleFor(x => x.Type)
            .NotEmpty()
            .WithMessage(x => $"Provider '{x.Name}': type is required")
            .Must(type => type is "trash-guides" or "config-templates" or "custom-formats")
            .WithMessage(x =>
                $"Provider '{x.Name}': type must be one of: trash-guides, config-templates, custom-formats"
            );

        When(
                x => x.Type == "custom-formats",
                () =>
                {
                    RuleFor(x => x.Service)
                        .Cascade(CascadeMode.Stop)
                        .NotEmpty()
                        .WithMessage(x =>
                            $"Provider '{x.Name}': service is required for custom-formats providers"
                        )
                        .Must(service => service is "radarr" or "sonarr")
                        .WithMessage(x =>
                            $"Provider '{x.Name}': service must be either 'radarr' or 'sonarr'"
                        );
                }
            )
            .Otherwise(() =>
            {
                RuleFor(x => x.Service)
                    .Empty()
                    .WithMessage(x =>
                        $"Provider '{x.Name}': service field is only allowed for custom-formats providers"
                    );
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
            .WithMessage(x => $"Provider '{x.Name}': clone_url must be a valid HTTP/HTTPS URL");

        RuleFor(x => x.Depth)
            .GreaterThanOrEqualTo(0)
            .WithMessage(x => $"Provider '{x.Name}': depth must be 0 or greater");
    }
}

public class LocalResourceProviderValidator : AbstractValidator<LocalResourceProvider>
{
    public LocalResourceProviderValidator()
    {
        RuleFor(x => x.Path).NotEmpty().WithMessage(x => $"Provider '{x.Name}': path is required");
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
