namespace Recyclarr.Cli.Console.Setup;

public interface IGlobalSetupTask
{
    void OnStart();
    void OnFinish();
}
