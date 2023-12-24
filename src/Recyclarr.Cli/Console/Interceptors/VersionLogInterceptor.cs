using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Interceptors;

public class VersionLogInterceptor(ILogger log) : ICommandInterceptor
{
    public void Intercept(CommandContext context, CommandSettings settings)
    {
        log.Debug("Recyclarr Version: {Version}", GitVersionInformation.InformationalVersion);
    }
}
