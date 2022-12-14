namespace Recyclarr.Common;

public interface IEnvironment
{
    public string GetFolderPath(Environment.SpecialFolder folder);
    string GetFolderPath(Environment.SpecialFolder folder, Environment.SpecialFolderOption option);
    string? GetEnvironmentVariable(string variable);
}
