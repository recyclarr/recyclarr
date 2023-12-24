using Recyclarr.Cli.Console.Setup;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Interceptors;

public class GlobalTaskInterceptor(IOrderedEnumerable<IGlobalSetupTask> tasks) : ICommandInterceptor
{
    public void Intercept(CommandContext context, CommandSettings settings)
    {
        tasks.ForEach(x => x.OnStart());
    }

    public void InterceptResult(CommandContext context, CommandSettings settings, ref int result)
    {
        tasks.Reverse().ForEach(x => x.OnFinish());
    }
}
