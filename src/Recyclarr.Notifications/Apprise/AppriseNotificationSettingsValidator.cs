using FluentValidation;
using Recyclarr.Common.FluentValidation;
using Recyclarr.Settings;

namespace Recyclarr.Notifications.Apprise;

public class AppriseNotificationSettingsValidator : AbstractValidator<AppriseNotificationSettings>
{
    public AppriseNotificationSettingsValidator()
    {
        RuleFor(x => x.Mode).NotNull();
        RuleFor(x => x.BaseUrl).NotEmpty();
    }
}
