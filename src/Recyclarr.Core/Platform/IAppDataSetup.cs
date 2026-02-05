namespace Recyclarr.Platform;

public interface IAppDataSetup
{
    public void SetConfigDirectoryOverride(string path);
    public void SetDataDirectoryOverride(string path);
}
