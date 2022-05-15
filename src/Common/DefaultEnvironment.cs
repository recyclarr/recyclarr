namespace Common;

internal class DefaultEnvironment : IEnvironment
{
    public string GetFolderPath(Environment.SpecialFolder folder)
    {
        return Environment.GetFolderPath(folder);
    }
}
