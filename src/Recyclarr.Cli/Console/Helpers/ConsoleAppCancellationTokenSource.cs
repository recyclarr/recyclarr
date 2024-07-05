namespace Recyclarr.Cli.Console.Helpers;

// Taken from: https://github.com/spectreconsole/spectre.console/issues/701#issuecomment-1081834778
internal sealed class ConsoleAppCancellationTokenSource : IDisposable
{
    private readonly ILogger _log;
    private readonly CancellationTokenSource _cts = new();

    public CancellationToken Token => _cts.Token;

    public ConsoleAppCancellationTokenSource(ILogger log)
    {
        _log = log;

        System.Console.CancelKeyPress += OnCancelKeyPress;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

        _cts.Token.Register(() =>
        {
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            System.Console.CancelKeyPress -= OnCancelKeyPress;
        });
    }

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        _log.Information("Exiting due to signal interrupt");

        // NOTE: cancel event, don't terminate the process
        e.Cancel = true;

        _cts.Cancel();
    }

    private void OnProcessExit(object? sender, EventArgs e)
    {
        if (_cts.IsCancellationRequested)
        {
            // NOTE: SIGINT (cancel key was pressed, this shouldn't ever actually hit however, as we remove the event
            // handler upon cancellation of the `cancellationSource`)
            return;
        }

        _cts.Cancel();
    }

    public void Dispose()
    {
        _cts.Dispose();
    }
}
