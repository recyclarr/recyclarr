namespace Recyclarr.TrashLib.Services.Processors;

public interface ISyncProcessor
{
    Task<ExitStatus> ProcessConfigs(ISyncSettings settings);
}
