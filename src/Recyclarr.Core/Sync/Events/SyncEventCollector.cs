using Recyclarr.Logging;
using Serilog.Context;

namespace Recyclarr.Sync.Events;

internal class SyncEventCollector : ISyncEventPublisher, IDisposable
{
    private readonly ILogger _log;
    private readonly SyncEventStorage _storage;
    private readonly IDisposable _subscription;
    private SyncContext _context;
    private IDisposable? _logContext;

    public SyncEventCollector(
        ILogger log,
        SyncEventStorage storage,
        ISyncContextSource contextSource
    )
    {
        _log = log;
        _storage = storage;
        _context = contextSource.Current;
        _subscription = contextSource.Subscribe(OnContextChanged);
    }

    private void OnContextChanged(SyncContext context)
    {
        // Update log context when instance changes
        if (context.InstanceName != _context.InstanceName)
        {
            _logContext?.Dispose();
            _logContext = context.InstanceName is not null
                ? LogContext.PushProperty(LogProperty.Scope, context.InstanceName)
                : null;
        }

        _context = context;
    }

    public void AddError(string message, Exception? exception = null)
    {
        _log.Error(exception, "{Message}", message);
        _storage.Add(
            new DiagnosticEvent(
                _context.InstanceName,
                _context.Pipeline,
                DiagnosticType.Error,
                message
            )
        );
    }

    public void AddWarning(string message)
    {
        _log.Warning("{Message}", message);
        _storage.Add(
            new DiagnosticEvent(
                _context.InstanceName,
                _context.Pipeline,
                DiagnosticType.Warning,
                message
            )
        );
    }

    public void AddDeprecation(string message)
    {
        _log.Warning("{Message}", message);
        _storage.Add(
            new DiagnosticEvent(
                _context.InstanceName,
                _context.Pipeline,
                DiagnosticType.Deprecation,
                message
            )
        );
    }

    public void Dispose()
    {
        _logContext?.Dispose();
        _subscription.Dispose();
    }
}
