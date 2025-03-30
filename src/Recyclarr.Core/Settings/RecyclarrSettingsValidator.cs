using FluentValidation;
using Recyclarr.Common.FluentValidation;
using Recyclarr.Settings.Models;

namespace Recyclarr.Settings;

public class RecyclarrSettingsValidator : AbstractValidator<RecyclarrSettings>
{
    public RecyclarrSettingsValidator()
    {
        RuleFor(x => x.Notifications).SetValidator(new NotificationSettingsValidator());
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
