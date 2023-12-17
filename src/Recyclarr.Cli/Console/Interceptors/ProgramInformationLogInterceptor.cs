using Recyclarr.Platform;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Interceptors;

public class ProgramInformationLogInterceptor(ILogger log, IAppPaths paths) : ICommandInterceptor
{
    public void Intercept(CommandContext context, CommandSettings settings)
    {
        log.Debug("Recyclarr Version: {Version}", GitVersionInformation.InformationalVersion);
        log.Debug("App Data Dir: {AppData}", paths.AppDataDirectory);
    }
}
