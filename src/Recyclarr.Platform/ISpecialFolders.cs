namespace Recyclarr.Platform;

/// <summary>
/// The operating system's special folders (application data, home). Environment variables are not
/// read here; they come from configuration (see <see cref="EnvironmentConfiguration"/>).
/// </summary>
public interface ISpecialFolders
{
    string GetFolderPath(
        Environment.SpecialFolder folder,
        Environment.SpecialFolderOption folderOption
    );
}

public sealed class DefaultSpecialFolders : ISpecialFolders
{
    public string GetFolderPath(
        Environment.SpecialFolder folder,
        Environment.SpecialFolderOption folderOption
    )
    {
        return Environment.GetFolderPath(folder, folderOption);
    }
}
