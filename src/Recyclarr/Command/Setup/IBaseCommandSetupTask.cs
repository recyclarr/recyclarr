namespace Recyclarr.Command.Setup;

public interface IBaseCommandSetupTask
{
    void OnStart();
    void OnFinish();
}
