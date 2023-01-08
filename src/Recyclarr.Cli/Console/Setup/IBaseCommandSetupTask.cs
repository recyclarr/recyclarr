namespace Recyclarr.Cli.Console.Setup;

public interface IBaseCommandSetupTask
{
    void OnStart();
    void OnFinish();
}
