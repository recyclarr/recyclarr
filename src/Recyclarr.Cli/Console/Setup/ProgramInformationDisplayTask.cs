using Recyclarr.Cli.Console.Commands;

namespace Recyclarr.Cli.Console.Setup;

internal class ProgramInformationDisplayTask(ILogger log) : IGlobalSetupTask
{
    public void OnStart(BaseCommandSettings cmd)
    {
        log.Debug("Recyclarr Version: {Version}", AppVersion.Informational);
    }

    public void OnFinish() { }
}
