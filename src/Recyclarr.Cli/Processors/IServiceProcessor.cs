using Recyclarr.Cli.Console.Settings;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.Cli.Processors;

public interface IServiceProcessor
{
    Task Process(ISyncSettings settings, IServiceConfiguration config);
}
