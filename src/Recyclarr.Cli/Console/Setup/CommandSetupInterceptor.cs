using Recyclarr.Cli.Console.Commands;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Setup;

internal sealed class CommandSetupInterceptor(Lazy<IGlobalSetupTask> globalTaskSetup)
    : ICommandInterceptor
{
    public void Intercept(CommandContext context, CommandSettings settings)
    {
        if (settings is not BaseCommandSettings cmd)
        {
            return;
        }

        globalTaskSetup.Value.OnStart(cmd);
    }

    public void InterceptResult(CommandContext context, CommandSettings settings, ref int result)
    {
        globalTaskSetup.Value.OnFinish();
    }
}
