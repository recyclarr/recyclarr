namespace Recyclarr.Config.Parsing.PostProcessing.Deprecations;

public interface IConfigDeprecationCheck
{
    ServiceConfigYaml Transform(ServiceConfigYaml include);
    bool CheckIfNeeded(ServiceConfigYaml include);
}
