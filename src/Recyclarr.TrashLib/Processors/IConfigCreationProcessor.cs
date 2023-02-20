namespace Recyclarr.TrashLib.Processors;

public interface IConfigCreationProcessor
{
    Task Process(string? configFilePath);
}
