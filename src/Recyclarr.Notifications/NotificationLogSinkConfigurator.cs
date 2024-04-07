using Recyclarr.Logging;
using Recyclarr.Settings;
using Serilog;
using Serilog.Events;
using Serilog.Templates;

namespace Recyclarr.Notifications;

public class NotificationLogSinkConfigurator(NotificationEmitter emitter, ISettingsProvider settingsProvider)
    : ILogConfigurator
{
    public void Configure(LoggerConfiguration config)
    {
        // If the user has disabled notifications, don't bother with adding the notification sink.
        if (settingsProvider.Settings.Notifications is null)
        {
            return;
        }

        var sink = new NotificationLogSink(emitter, BuildExpressionTemplate());
        config.WriteTo.Sink(sink, LogEventLevel.Information);
    }

    private static ExpressionTemplate BuildExpressionTemplate()
    {
        return new ExpressionTemplate(LogTemplates.Base);
    }
}
