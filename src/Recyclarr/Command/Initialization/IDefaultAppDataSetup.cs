namespace Recyclarr.Command.Initialization;

public interface IDefaultAppDataSetup
{
    void SetupDefaultPath(bool forceCreate = false);
}
