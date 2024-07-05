using Recyclarr.Cli.Console.Settings;

namespace Recyclarr.Cli.Processors.Sync;

public interface ISyncProcessor
{
    Task<ExitStatus> ProcessConfigs(ISyncSettings settings, CancellationToken ct);
}
