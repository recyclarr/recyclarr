namespace Recyclarr.Config.Parsing.PostProcessing;

public interface IConfigPostProcessor
{
    RootConfigYaml Process(RootConfigYaml config);
}
