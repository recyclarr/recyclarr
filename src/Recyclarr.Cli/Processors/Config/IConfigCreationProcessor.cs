using Recyclarr.Config;

namespace Recyclarr.Cli.Processors.Config;

internal interface IConfigCreationProcessor
{
    void Process(ICreateConfigSettings settings);
}
