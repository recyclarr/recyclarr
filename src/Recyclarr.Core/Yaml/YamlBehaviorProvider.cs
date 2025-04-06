using System.Reflection;

namespace Recyclarr.Yaml;

internal class YamlBehaviorProvider(IEnumerable<IYamlBehavior> behaviors)
{
    public IEnumerable<IYamlBehavior> GetBehaviors(YamlFileType yamlFileType)
    {
        return behaviors.Where(x =>
        {
            var types = x.GetType().GetCustomAttribute<ForYamlFileTypesAttribute>()?.FileTypes;
            return types?.Contains(yamlFileType) ?? true;
        });
    }
}
