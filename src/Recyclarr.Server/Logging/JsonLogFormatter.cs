using System.Globalization;
using Recyclarr.Logging;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using Serilog.Formatting.Json;

namespace Recyclarr.Server.Logging;

/// <summary>
/// Writes one JSON object per line for log collectors. Top-level keys follow OpenTelemetry names
/// for logs that are not OTLP (<c>trace_id</c>, <c>span_id</c>, <c>exception.*</c>), so collectors
/// can correlate log lines with traces without a custom parser.
/// </summary>
/// <remarks>
/// <c>exception.message</c> is the sanitized message from <see cref="ExceptionSanitizingEnricher"/>
/// and <c>exception.stacktrace</c> omits the message, so no unsanitized exception text is written.
/// Remaining event properties follow as top-level keys.
/// </remarks>
internal sealed class JsonLogFormatter : ITextFormatter
{
    private const string SanitizedMessageProperty = "SanitizedExceptionMessage";

    private static readonly JsonValueFormatter ValueFormatter = new(typeTagName: null);

    private static readonly MessageTemplateTextFormatter MessageFormatter = new(
        "{Message:lj}",
        CultureInfo.InvariantCulture
    );

    public void Format(LogEvent logEvent, TextWriter output)
    {
        output.Write("{\"timestamp\":\"");
        output.Write(logEvent.Timestamp.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
        output.Write("\",\"level\":\"");
        output.Write(LevelName(logEvent.Level));
        output.Write('"');

        WriteString(output, "message", RenderMessage(logEvent));
        WriteString(output, "message_template", logEvent.MessageTemplate.Text);

        if (logEvent.TraceId is { } traceId)
        {
            WriteString(output, "trace_id", traceId.ToHexString());
        }

        if (logEvent.SpanId is { } spanId)
        {
            WriteString(output, "span_id", spanId.ToHexString());
        }

        if (logEvent.Exception is { } exception)
        {
            WriteString(output, "exception.type", exception.GetType().FullName ?? "");

            if (
                logEvent.Properties.TryGetValue(SanitizedMessageProperty, out var message)
                && message is ScalarValue { Value: string text }
            )
            {
                WriteString(output, "exception.message", text);
            }

            if (exception.StackTrace is { } stackTrace)
            {
                WriteString(output, "exception.stacktrace", stackTrace);
            }
        }

        foreach (var (name, value) in logEvent.Properties)
        {
            if (name == LogProperty.Scope || name == SanitizedMessageProperty)
            {
                continue;
            }

            output.Write(',');
            JsonValueFormatter.WriteQuotedJsonString(name, output);
            output.Write(':');
            ValueFormatter.Format(value, output);
        }

        output.Write('}');
        output.Write('\n');
    }

    private static string RenderMessage(LogEvent logEvent)
    {
        // `l` renders strings without quotes, matching the text console; `j` renders structured
        // values as JSON.
        using var rendered = new StringWriter(CultureInfo.InvariantCulture);
        MessageFormatter.Format(logEvent, rendered);
        var message = rendered.ToString();
        return
            logEvent.Properties.TryGetValue(LogProperty.Scope, out var scope)
            && scope is ScalarValue { Value: string scopeText }
            ? $"{scopeText}: {message}"
            : message;
    }

    private static void WriteString(TextWriter output, string name, string value)
    {
        output.Write(",\"");
        output.Write(name);
        output.Write("\":");
        JsonValueFormatter.WriteQuotedJsonString(value, output);
    }

    private static string LevelName(LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Verbose => "trace",
            LogEventLevel.Debug => "debug",
            LogEventLevel.Information => "info",
            LogEventLevel.Warning => "warn",
            LogEventLevel.Error => "error",
            _ => "fatal",
        };
    }
}
