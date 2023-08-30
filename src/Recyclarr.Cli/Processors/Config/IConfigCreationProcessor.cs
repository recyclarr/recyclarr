using Recyclarr.Cli.Console.Settings;

namespace Recyclarr.Cli.Processors.Config;

public interface IConfigCreationProcessor
{
    void Process(ICreateConfigSettings settings);
}
