using Recyclarr.Cli.Console.Commands;

namespace Recyclarr.Cli.Console.Setup;

internal interface IGlobalSetupTask
{
    void OnStart(BaseCommandSettings cmd);
    void OnFinish();
}
