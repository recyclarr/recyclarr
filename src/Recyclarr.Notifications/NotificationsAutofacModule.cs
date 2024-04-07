using Autofac;
using Recyclarr.Notifications.Apprise;
using Serilog.Core;

namespace Recyclarr.Notifications;

public class NotificationsAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        builder.RegisterAssemblyTypes(ThisAssembly)
            .AssignableTo<ILogEventSink>()
            .As<ILogEventSink>();

        builder.RegisterType<NotificationService>().SingleInstance();
        builder.RegisterType<NotificationEmitter>().SingleInstance();

        // Apprise
        builder.RegisterType<AppriseNotificationApiService>().As<IAppriseNotificationApiService>();
        builder.RegisterType<AppriseRequestBuilder>().As<IAppriseRequestBuilder>();
    }
}
