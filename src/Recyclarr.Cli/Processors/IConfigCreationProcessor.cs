namespace Recyclarr.Cli.Processors;

public interface IConfigCreationProcessor
{
    Task Process(string? configFilePath);
}
