namespace Recyclarr.Platform;

public class DefaultEnvironment : IEnvironment
{
    public string GetFolderPath(Environment.SpecialFolder folder)
    {
        return Environment.GetFolderPath(folder);
    }

    public string GetFolderPath(Environment.SpecialFolder folder, Environment.SpecialFolderOption folderOption)
    {
        return Environment.GetFolderPath(folder, folderOption);
    }

    public string? GetEnvironmentVariable(string variable)
    {
        return Environment.GetEnvironmentVariable(variable);
    }
}
