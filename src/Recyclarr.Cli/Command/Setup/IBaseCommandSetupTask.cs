namespace Recyclarr.Cli.Command.Setup;

public interface IBaseCommandSetupTask
{
    void OnStart();
    void OnFinish();
}
