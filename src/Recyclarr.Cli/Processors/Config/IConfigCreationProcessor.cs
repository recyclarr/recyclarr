using Recyclarr.Cli.Console.Settings;

namespace Recyclarr.Cli.Processors.Config;

public interface IConfigCreationProcessor
{
    Task Process(ICreateConfigSettings settings);
}
