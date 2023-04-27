using Recyclarr.Cli.Console.Settings;

namespace Recyclarr.Cli.Processors;

public interface ISyncProcessor
{
    Task<ExitStatus> ProcessConfigs(ISyncSettings settings);
}
