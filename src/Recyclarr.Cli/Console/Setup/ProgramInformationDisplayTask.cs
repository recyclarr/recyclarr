using Recyclarr.Cli.Console.Commands;
using Recyclarr.Platform;

namespace Recyclarr.Cli.Console.Setup;

public class ProgramInformationDisplayTask(ILogger log, IAppPaths paths) : IGlobalSetupTask
{
    public void OnStart(BaseCommandSettings cmd)
    {
        log.Debug("Recyclarr Version: {Version}", GitVersionInformation.InformationalVersion);
        log.Debug("App Data Dir: {AppData}", paths.AppDataDirectory);
    }

    public void OnFinish()
    {
    }
}
