namespace Recyclarr.TrashLib.Services.Processors;

public interface IServiceProcessor
{
    Task Process(ISyncSettings settings);
}
