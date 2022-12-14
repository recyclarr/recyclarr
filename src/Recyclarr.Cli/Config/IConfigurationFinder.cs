namespace Recyclarr.Cli.Config;

public interface IConfigurationFinder
{
    IReadOnlyCollection<string> GetConfigFiles(IReadOnlyCollection<string>? configs);
}
