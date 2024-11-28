using Recyclarr.Cli.Console.Commands;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Setup;

internal sealed class CommandSetupInterceptor : ICommandInterceptor, IDisposable
{
    private readonly ConsoleAppCancellationTokenSource _ct = new();
    private readonly Lazy<IGlobalSetupTask> _globalTaskSetup;

    public CommandSetupInterceptor(Lazy<ILogger> log, Lazy<IGlobalSetupTask> globalTaskSetup)
    {
        _globalTaskSetup = globalTaskSetup;
        _ct.CancelPressed.Subscribe(_ => log.Value.Information("Exiting due to signal interrupt"));
    }

    // Executed on CLI startup
    public void Intercept(CommandContext context, CommandSettings settings)
    {
        if (settings is not BaseCommandSettings cmd)
        {
            throw new InvalidOperationException(
                "Command settings must be of type BaseCommandSettings"
            );
        }

        cmd.CancellationToken = _ct.Token;
        _globalTaskSetup.Value.OnStart(cmd);
    }

    // Executed on CLI exit
    public void InterceptResult(CommandContext context, CommandSettings settings, ref int result)
    {
        _globalTaskSetup.Value.OnFinish();
    }

    public void Dispose()
    {
        _ct.Dispose();
    }
}
