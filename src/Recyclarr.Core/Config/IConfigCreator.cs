namespace Recyclarr.Config;

public interface IConfigCreator
{
    bool CanHandle(ICreateConfigSettings settings);
    IReadOnlyList<CreatedConfigFile> Create(ICreateConfigSettings settings);
}
