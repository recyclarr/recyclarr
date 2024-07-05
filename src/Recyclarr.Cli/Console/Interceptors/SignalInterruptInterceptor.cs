using Recyclarr.Cli.Console.Helpers;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Interceptors;

// Inspired by: https://github.com/spectreconsole/spectre.console/issues/701#issuecomment-1081834778
internal sealed class SignalInterruptInterceptor(
    ILogger log,
    ConsoleAppCancellationTokenProvider tokenProvider)
    : ICommandInterceptor, IDisposable
{
    private CancellationTokenSource? _cts;

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(_cts);

        // NOTE: cancel event, don't terminate the process
        e.Cancel = true;

        _cts.Cancel();
    }

    private void OnProcessExit(object? sender, EventArgs e)
    {
        ArgumentNullException.ThrowIfNull(_cts);

        if (_cts.IsCancellationRequested)
        {
            // NOTE: SIGINT (cancel key was pressed, this shouldn't ever actually hit however, as we remove the event
            // handler upon cancellation of the `cancellationSource`)
            return;
        }

        _cts.Cancel();
    }

    public void Intercept(CommandContext context, CommandSettings settings)
    {
        _cts = new CancellationTokenSource();

        System.Console.CancelKeyPress += OnCancelKeyPress;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

        _cts.Token.Register(() =>
        {
            log.Information("Exiting due to signal interrupt");
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            System.Console.CancelKeyPress -= OnCancelKeyPress;
        });

        tokenProvider.SetToken(_cts.Token);
    }

    private void Cancel()
    {
        tokenProvider.ResetToken();
        _cts?.Cancel();
    }

    public void InterceptResult(CommandContext context, CommandSettings settings, ref int result)
    {
        ArgumentNullException.ThrowIfNull(_cts);
        Cancel();
    }

    public void Dispose()
    {
        // Repeat the call to Cancel() here in case exceptions occur in the command class and those exceptions leak
        // through to Spectre.Console. It won't invoke InterceptResult() on exceptions.
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
