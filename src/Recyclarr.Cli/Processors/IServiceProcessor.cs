using Recyclarr.Cli.Console.Settings;
using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Processors;

public interface IServiceProcessor
{
    Task Process(ISyncSettings settings, IServiceConfiguration config);
}
