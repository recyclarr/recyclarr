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
    private readonly Lazy<GlobalSetupTaskExecutor> _taskExecutor;

    public CommandSetupInterceptor(
        Lazy<ILogger> log,
        LoggingLevelSwitch loggingLevelSwitch,
        IAppDataSetup appDataSetup,
        Lazy<GlobalSetupTaskExecutor> taskExecutor)
    {
        _loggingLevelSwitch = loggingLevelSwitch;
        _appDataSetup = appDataSetup;
        _taskExecutor = taskExecutor;

        _ct.CancelPressed.Subscribe(_ => log.Value.Information("Exiting due to signal interrupt"));
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

        _taskExecutor.Value.OnStart();
    }

    public void InterceptResult(CommandContext context, CommandSettings settings, ref int result)
    {
        _taskExecutor.Value.OnFinish();
    }

    private void HandleServiceCommand(ServiceCommandSettings cmd)
    {
        HandleBaseCommand(cmd);
        _appDataSetup.SetAppDataDirectoryOverride(cmd.AppData ?? "");
    }

    private void HandleBaseCommand(BaseCommandSettings cmd)
    {
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
