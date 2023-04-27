using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Recyclarr.Cli.Console.Commands;
using Recyclarr.TrashLib.Startup;
using Serilog.Core;
using Serilog.Events;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Setup;

public class CliInterceptor : ICommandInterceptor
{
    private readonly LoggingLevelSwitch _loggingLevelSwitch;
    private readonly AppDataPathProvider _appDataPathProvider;
    private readonly Subject<Unit> _interceptedSubject = new();

    public IObservable<Unit> OnIntercepted => _interceptedSubject.AsObservable();

    public CliInterceptor(LoggingLevelSwitch loggingLevelSwitch, AppDataPathProvider appDataPathProvider)
    {
        _loggingLevelSwitch = loggingLevelSwitch;
        _appDataPathProvider = appDataPathProvider;
    }

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

        _appDataPathProvider.AppDataPath = cmd.AppData;
    }

    private void HandleBaseCommand(BaseCommandSettings cmd)
    {
        _loggingLevelSwitch.MinimumLevel = cmd.Debug switch
        {
            true => LogEventLevel.Debug,
            _ => LogEventLevel.Information
        };
    }
}
