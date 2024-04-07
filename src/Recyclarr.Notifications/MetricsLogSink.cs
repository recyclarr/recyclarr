using System.Diagnostics.CodeAnalysis;
using Serilog.Core;
using Serilog.Events;

namespace Recyclarr.Notifications;

public class MetricsLogSink(NotificationEmitter metrics) : ILogEventSink
{
    [SuppressMessage("ReSharper", "SwitchStatementMissingSomeEnumCasesNoDefault", Justification =
        "Only processes warnings & errors")]
    public void Emit(LogEvent logEvent)
    {
        switch (logEvent.Level)
        {
            case LogEventLevel.Warning:
                metrics.WarningIssued();
                break;

            case LogEventLevel.Error:
            case LogEventLevel.Fatal:
                metrics.ErrorIssued();
                break;
        }
    }
}
