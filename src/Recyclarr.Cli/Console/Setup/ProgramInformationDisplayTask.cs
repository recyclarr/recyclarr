using Recyclarr.Platform;

namespace Recyclarr.Cli.Console.Setup;

public class ProgramInformationDisplayTask(ILogger log, IAppPaths paths) : IGlobalSetupTask
{
    public void OnStart()
    {
        log.Debug("Recyclarr Version: {Version}", GitVersionInformation.InformationalVersion);
        log.Debug("App Data Dir: {AppData}", paths.AppDataDirectory);
    }
}
