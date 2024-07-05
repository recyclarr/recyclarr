using Recyclarr.Cli.Console.Commands;
using Recyclarr.Platform;
using Serilog.Core;
using Serilog.Events;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Interceptors;

internal sealed class BaseCommandSetupInterceptor(LoggingLevelSwitch loggingLevelSwitch, IAppDataSetup appDataSetup)
    : ICommandInterceptor
{
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
    }

    private void HandleServiceCommand(ServiceCommandSettings cmd)
    {
        HandleBaseCommand(cmd);
        appDataSetup.AppDataDirectoryOverride = cmd.AppData;
    }

    private void HandleBaseCommand(BaseCommandSettings cmd)
    {
        loggingLevelSwitch.MinimumLevel = cmd.Debug switch
        {
            true => LogEventLevel.Debug,
            _ => LogEventLevel.Information
        };
    }
}
