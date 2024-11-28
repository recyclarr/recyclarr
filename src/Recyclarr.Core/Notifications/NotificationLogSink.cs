using System.Diagnostics.CodeAnalysis;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace Recyclarr.Notifications;

public class NotificationLogSink(NotificationEmitter emitter, ITextFormatter formatter)
    : ILogEventSink
{
    [SuppressMessage(
        "ReSharper",
        "SwitchStatementMissingSomeEnumCasesNoDefault",
        Justification = "Only processes warnings & errors"
    )]
    public void Emit(LogEvent logEvent)
    {
        switch (logEvent.Level)
        {
            case LogEventLevel.Warning:
                emitter.SendWarning(RenderLogEvent(logEvent));
                break;

            case LogEventLevel.Error:
            case LogEventLevel.Fatal:
                emitter.SendError(RenderLogEvent(logEvent));
                break;
        }
    }

    private string RenderLogEvent(LogEvent logEvent)
    {
        using var writer = new StringWriter();
        formatter.Format(logEvent, writer);
        return writer.ToString().Trim();
    }
}
