using System.Diagnostics.Metrics;

namespace Recyclarr.Notifications;

public class NotificationEmitter
{
    private readonly IMeterFactory _meterFactory;
    private readonly Dictionary<string, Counter<int>> _counters = new();

    private Counter<int> _counterWarnings = null!;
    private Counter<int> _counterErrors = null!;

    public Meter Meter { get; private set; } = null!;

    public NotificationEmitter(IMeterFactory meterFactory)
    {
        _meterFactory = meterFactory;
        BeginCollecting("Global");
    }

    public void BeginCollecting(string scope)
    {
        Meter = _meterFactory.Create(scope);
        _counterWarnings = Meter.CreateCounter<int>("warnings", description: "Number of warnings issued");
        _counterErrors = Meter.CreateCounter<int>("errors", description: "Number of errors issued");
    }

    public void WarningIssued()
    {
        _counterWarnings.Add(1);
    }

    public void ErrorIssued()
    {
        _counterErrors.Add(1);
    }

    public void NotifyStatistic(string name, int totalCount)
    {
        if (!_counters.TryGetValue(name, out var counter))
        {
            counter = Meter.CreateCounter<int>(name);
            _counters.Add(name, counter);
        }

        counter.Add(totalCount);
    }
}
