using Recyclarr.Cli.Console.Commands;

namespace Recyclarr.Cli.Console.Setup;

public interface IGlobalSetupTask
{
    void OnStart(BaseCommandSettings cmd);
    void OnFinish();
}
