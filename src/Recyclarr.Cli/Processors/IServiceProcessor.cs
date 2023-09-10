using Recyclarr.Cli.Console.Settings;
using Recyclarr.TrashLib.Config;

namespace Recyclarr.Cli.Processors;

public interface IServiceProcessor
{
    Task Process(ISyncSettings settings, IServiceConfiguration config);
}
