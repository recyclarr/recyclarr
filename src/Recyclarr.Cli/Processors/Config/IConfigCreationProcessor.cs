namespace Recyclarr.Cli.Processors.Config;

public interface IConfigCreationProcessor
{
    Task Process(string? configFilePath);
}
