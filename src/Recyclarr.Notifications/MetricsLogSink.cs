using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Serilog.Core;
using Serilog.Events;

namespace Recyclarr.Notifications;

public class MetricsLogSink : ILogEventSink
{
    private readonly Counter<int> _warningCount;
    private readonly Counter<int> _errorCount;

    public MetricsLogSink(Meter metrics)
    {
        var meter = meterFactory.Create();
    }

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
