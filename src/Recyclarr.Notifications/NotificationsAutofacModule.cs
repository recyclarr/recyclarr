using Autofac;
using Recyclarr.Logging;
using Recyclarr.Notifications.Apprise;

namespace Recyclarr.Notifications;

public class NotificationsAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        builder.RegisterType<NotificationLogSinkConfigurator>().As<ILogConfigurator>();
        builder.RegisterType<NotificationService>().SingleInstance();
        builder.RegisterType<NotificationEmitter>().SingleInstance();

        // Apprise
        builder.RegisterType<AppriseNotificationApiService>().As<IAppriseNotificationApiService>();
        builder.RegisterType<AppriseRequestBuilder>().As<IAppriseRequestBuilder>();
    }
}
