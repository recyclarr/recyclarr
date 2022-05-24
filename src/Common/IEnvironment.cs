namespace Common;

public interface IEnvironment
{
    public string GetFolderPath(Environment.SpecialFolder folder);
    string GetFolderPath(Environment.SpecialFolder folder, Environment.SpecialFolderOption option);
}
