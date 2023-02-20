namespace Recyclarr.TrashLib.Processors;

public interface ISyncProcessor
{
    Task<ExitStatus> ProcessConfigs(ISyncSettings settings);
}
