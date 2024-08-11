using Recyclarr.Cli.Console.Commands;
using Recyclarr.Cli.Console.Setup;
using Recyclarr.Platform;
using Serilog.Core;
using Serilog.Events;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Helpers;

internal sealed class CommandSetupInterceptor : ICommandInterceptor, IDisposable
{
    private readonly ConsoleAppCancellationTokenSource _ct = new();
    private readonly LoggingLevelSwitch _loggingLevelSwitch;
    private readonly IAppDataSetup _appDataSetup;
    private readonly Lazy<IGlobalSetupTask> _globalTaskSetup;

    public CommandSetupInterceptor(
        Lazy<ILogger> log,
        LoggingLevelSwitch loggingLevelSwitch,
        IAppDataSetup appDataSetup,
        Lazy<IGlobalSetupTask> globalTaskSetup)
    {
        _loggingLevelSwitch = loggingLevelSwitch;
        _appDataSetup = appDataSetup;
        _globalTaskSetup = globalTaskSetup;

        _ct.CancelPressed.Subscribe(_ => log.Value.Information("Exiting due to signal interrupt"));
    }

    // Executed on CLI startup
    public void Intercept(CommandContext context, CommandSettings settings)
    {
        switch (settings)
        {
            case BaseCommandSettings cmd:
                HandleBaseCommand(cmd);
                break;
        }

        _globalTaskSetup.Value.OnStart();
    }

    // Executed on CLI exit
    public void InterceptResult(CommandContext context, CommandSettings settings, ref int result)
    {
        _globalTaskSetup.Value.OnFinish();
    }

    private void HandleBaseCommand(BaseCommandSettings cmd)
    {
        _appDataSetup.SetAppDataDirectoryOverride(cmd.AppData ?? "");

        cmd.CancellationToken = _ct.Token;

        _loggingLevelSwitch.MinimumLevel = cmd.Debug switch
        {
            true => LogEventLevel.Debug,
            _ => LogEventLevel.Information
        };
    }

    public void Dispose()
    {
        _ct.Dispose();
    }
}
