namespace Recyclarr.Command.Initialization;

public interface IDefaultAppDataSetup
{
    void SetupDefaultPath(string? appDataDirectoryOverride, bool forceCreate);
}
