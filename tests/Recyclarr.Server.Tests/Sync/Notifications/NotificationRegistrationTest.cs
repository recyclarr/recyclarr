using Autofac;
using Recyclarr.Server.Sync.Notifications;
using Recyclarr.Server.Tests.Reusable;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.Server.Tests.Sync.Notifications;

internal sealed class NotificationRegistrationTest : ServerIntegrationFixture
{
    [Test]
    public void Unconfigured_notifications_resolve_the_noop_service()
    {
        using var scope = Container.BeginLifetimeScope("run");

        scope.Resolve<INotificationService>().Should().BeOfType<NoopNotificationService>();
    }

    [Test]
    public void Configured_notifications_resolve_the_result_service()
    {
        var settings = Substitute.For<ISettings<NotificationSettings>>();
        settings.Value.Returns(
            new NotificationSettings
            {
                Apprise = new AppriseNotificationSettings
                {
                    Mode = AppriseMode.Stateful,
                    BaseUrl = new Uri("http://apprise.local"),
                },
            }
        );
        using var scope = Container.BeginLifetimeScope(
            "run",
            builder => builder.RegisterInstance(settings).As<ISettings<NotificationSettings>>()
        );

        scope.Resolve<INotificationService>().Should().BeOfType<NotificationService>();
    }
}
