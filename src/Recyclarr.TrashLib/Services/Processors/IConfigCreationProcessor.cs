namespace Recyclarr.TrashLib.Services.Processors;

public interface IConfigCreationProcessor
{
    Task Process(string? configFilePath);
}
