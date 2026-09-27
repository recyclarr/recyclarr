using Recyclarr.Cli.Console.Commands;

namespace Recyclarr.Cli.Console.Setup;

internal class ProgramInformationDisplayTask(ILogger log) : IGlobalSetupTask
{
    public void OnStart(BaseCommandSettings cmd)
    {
        log.Debug("Recyclarr Version: {Version}", GitVersionInformation.InformationalVersion);
    }

    public void OnFinish() { }
}
