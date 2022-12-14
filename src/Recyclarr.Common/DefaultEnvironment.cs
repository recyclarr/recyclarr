namespace Recyclarr.Common;

internal class DefaultEnvironment : IEnvironment
{
    public string GetFolderPath(Environment.SpecialFolder folder)
    {
        return Environment.GetFolderPath(folder);
    }

    public string GetFolderPath(Environment.SpecialFolder folder, Environment.SpecialFolderOption option)
    {
        return Environment.GetFolderPath(folder, option);
    }

    public string? GetEnvironmentVariable(string variable)
    {
        return Environment.GetEnvironmentVariable(variable);
    }
}
