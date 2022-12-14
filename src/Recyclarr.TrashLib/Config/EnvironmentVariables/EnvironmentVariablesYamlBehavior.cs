using JetBrains.Annotations;
using Recyclarr.Common;
using Recyclarr.TrashLib.Config.Yaml;
using YamlDotNet.Serialization;

namespace Recyclarr.TrashLib.Config.EnvironmentVariables;

[UsedImplicitly]
public class EnvironmentVariablesYamlBehavior : IYamlBehavior
{
    private readonly IEnvironment _environment;

    public EnvironmentVariablesYamlBehavior(IEnvironment environment)
    {
        _environment = environment;
    }

    public void Setup(DeserializerBuilder builder)
    {
        builder
            .WithNodeDeserializer(new EnvironmentVariablesDeserializer(_environment))
            .WithTagMapping("!env_var", typeof(EnvironmentVariableTag));
    }
}
