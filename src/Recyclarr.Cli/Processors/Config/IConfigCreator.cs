using Recyclarr.Config;

namespace Recyclarr.Cli.Processors.Config;

internal interface IConfigCreator
{
    bool CanHandle(ICreateConfigSettings settings);
    void Create(ICreateConfigSettings settings);
}
