using Autofac;
using Recyclarr.Logging;
using Recyclarr.Notifications.Apprise;
using Recyclarr.Settings;

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
        builder.RegisterType<AppriseStatefulNotificationApiService>()
            .Keyed<IAppriseNotificationApiService>(AppriseMode.Stateful);

        builder.RegisterType<AppriseStatelessNotificationApiService>()
            .Keyed<IAppriseNotificationApiService>(AppriseMode.Stateless);

        builder.RegisterType<AppriseRequestBuilder>().As<IAppriseRequestBuilder>();
    }
}
