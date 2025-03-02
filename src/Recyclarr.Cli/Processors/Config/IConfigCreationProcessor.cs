using Recyclarr.Cli.Console.Settings;

namespace Recyclarr.Cli.Processors.Config;

internal interface IConfigCreationProcessor
{
    void Process(ICreateConfigSettings settings);
}
