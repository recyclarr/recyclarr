using System.Diagnostics;
using System.Text.Json;
using Recyclarr.Logging;
using Recyclarr.Server.Logging;

namespace Recyclarr.Server.Tests.Logging;

/// <summary>
/// Log collectors parse each console line as one JSON object; key names follow OpenTelemetry's
/// names for logs that are not OTLP.
/// </summary>
internal sealed class JsonLogFormatterTest
{
    [Test]
    public void Event_is_one_json_line_with_rendered_message_and_properties()
    {
        var line = WriteSingleLine(log =>
            log.ForContext(LogProperty.Scope, "radarr")
                .Warning("Synced {Count} items to {Instance}", 3, "movies")
        );

        using var json = JsonDocument.Parse(line);
        var root = json.RootElement;
        root.GetProperty("level").GetString().Should().Be("warn");
        root.GetProperty("message").GetString().Should().Be("radarr: Synced 3 items to movies");
        root.GetProperty("message_template")
            .GetString()
            .Should()
            .Be("Synced {Count} items to {Instance}");
        root.GetProperty("Count").GetInt32().Should().Be(3);
        root.GetProperty("timestamp").GetString().Should().EndWith("Z");
        root.TryGetProperty("trace_id", out _).Should().BeFalse();
        root.TryGetProperty(LogProperty.Scope, out _).Should().BeFalse();
    }

    [Test]
    public void Event_inside_an_activity_carries_its_trace_and_span_ids()
    {
        using var activity = new Activity("test");
        activity.Start();

        var line = WriteSingleLine(log => log.Information("traced"));

        using var json = JsonDocument.Parse(line);
        json.RootElement.GetProperty("trace_id")
            .GetString()
            .Should()
            .Be(activity.TraceId.ToHexString());
        json.RootElement.GetProperty("span_id")
            .GetString()
            .Should()
            .Be(activity.SpanId.ToHexString());
    }

    [Test]
    public void Exception_is_written_without_its_unsanitized_message()
    {
        var line = WriteSingleLine(log => log.Error(Thrown("https://private-host/api"), "failed"));

        line.Should().NotContain("private-host");
        using var json = JsonDocument.Parse(line);
        var root = json.RootElement;
        root.GetProperty("level").GetString().Should().Be("error");
        root.GetProperty("exception.type")
            .GetString()
            .Should()
            .Be(typeof(InvalidOperationException).FullName);
        root.GetProperty("exception.message").GetString().Should().NotBeNullOrEmpty();
        root.GetProperty("exception.stacktrace").GetString().Should().Contain(nameof(Thrown));
    }

    private static string WriteSingleLine(Action<ILogger> write)
    {
        using var output = new StringWriter();
        using (
            var log = LogSetup
                .BaseConfiguration()
                .WriteTo.TextWriter(new JsonLogFormatter(), output)
                .CreateLogger()
        )
        {
            write(log);
        }

        var text = output.ToString();
        text.Should().EndWith("\n").And.Subject.Count(c => c == '\n').Should().Be(1);
        return text.TrimEnd('\n');
    }

    private static InvalidOperationException Thrown(string message)
    {
        try
        {
            throw new InvalidOperationException(message);
        }
        catch (InvalidOperationException e)
        {
            return e;
        }
    }
}
