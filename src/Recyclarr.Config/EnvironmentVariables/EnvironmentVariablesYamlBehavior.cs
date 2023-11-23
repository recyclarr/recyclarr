using JetBrains.Annotations;
using Recyclarr.Platform;
using Recyclarr.Yaml;
using YamlDotNet.Serialization;

namespace Recyclarr.Config.EnvironmentVariables;

[UsedImplicitly]
public class EnvironmentVariablesYamlBehavior(IEnvironment environment) : IYamlBehavior
{
    public void Setup(DeserializerBuilder builder)
    {
        builder
            .WithNodeDeserializer(new EnvironmentVariablesDeserializer(environment))
            .WithTagMapping("!env_var", typeof(EnvironmentVariableTag));
    }
}
