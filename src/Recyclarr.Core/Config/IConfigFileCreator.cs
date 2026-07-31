namespace Recyclarr.Config;

public interface IConfigFileCreator
{
    IReadOnlyList<CreatedConfigFile> Create(ICreateConfigSettings settings);
}
