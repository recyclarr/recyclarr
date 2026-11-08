using System.Collections.Concurrent;
using System.Diagnostics;
using Recyclarr.Server.Observability;

namespace Recyclarr.Server.Tests.Reusable;

/// <summary>
/// Records spans from the Recyclarr and ASP.NET Core sources. Listening to ASP.NET Core makes the
/// server start a span for each request, as it does when tracing is exported. Tests run in
/// parallel, so each test looks up only the span of its own job.
/// </summary>
internal sealed class RecordedSpans : IDisposable
{
    private readonly ConcurrentBag<Activity> _stopped = [];
    private readonly ActivityListener _listener;

    public RecordedSpans()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source =>
                source.Name is ServerTelemetry.SourceName or "Microsoft.AspNetCore",
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = _stopped.Add,
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public Activity ForJob(Guid jobId)
    {
        var id = jobId.ToString();
        return _stopped
            .Should()
            .ContainSingle(a => (string?)a.GetTagItem("recyclarr.sync.job.id") == id)
            .Subject;
    }

    public void Dispose()
    {
        _listener.Dispose();
    }
}
