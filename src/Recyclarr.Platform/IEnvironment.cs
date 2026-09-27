namespace Recyclarr.Platform;

public interface IEnvironment
{
    string GetFolderPath(Environment.SpecialFolder folder);
    string GetFolderPath(
        Environment.SpecialFolder folder,
        Environment.SpecialFolderOption folderOption
    );

    string? GetEnvironmentVariable(string variable);
}
