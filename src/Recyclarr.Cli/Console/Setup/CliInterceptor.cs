using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Recyclarr.Cli.Console.Commands;
using Recyclarr.Cli.Console.Helpers;
using Recyclarr.Platform;
using Serilog.Core;
using Serilog.Events;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Setup;

public class CliInterceptor(LoggingLevelSwitch loggingLevelSwitch, AppDataPathProvider appDataPathProvider)
    : ICommandInterceptor
{
    private readonly Subject<Unit> _interceptedSubject = new();
    private readonly ConsoleAppCancellationTokenSource _ct = new();

    public IObservable<Unit> OnIntercepted => _interceptedSubject.AsObservable();

    public void Intercept(CommandContext context, CommandSettings settings)
    {
        switch (settings)
        {
            case ServiceCommandSettings cmd:
                HandleServiceCommand(cmd);
                break;

            case BaseCommandSettings cmd:
                HandleBaseCommand(cmd);
                break;
        }

        _interceptedSubject.OnNext(Unit.Default);
        _interceptedSubject.OnCompleted();
    }

    private void HandleServiceCommand(ServiceCommandSettings cmd)
    {
        HandleBaseCommand(cmd);

        appDataPathProvider.AppDataPath = cmd.AppData;
    }

    private void HandleBaseCommand(BaseCommandSettings cmd)
    {
        cmd.CancellationToken = _ct.Token;

        loggingLevelSwitch.MinimumLevel = cmd.Debug switch
        {
            true => LogEventLevel.Debug,
            _ => LogEventLevel.Information
        };
    }
}
