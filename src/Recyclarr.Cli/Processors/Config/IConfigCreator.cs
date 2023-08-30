using Recyclarr.Cli.Console.Settings;

namespace Recyclarr.Cli.Processors.Config;

public interface IConfigCreator
{
    bool CanHandle(ICreateConfigSettings settings);
    void Create(ICreateConfigSettings settings);
}
