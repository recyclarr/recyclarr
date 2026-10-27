using Microsoft.Extensions.Configuration;
using Recyclarr.Yaml;
using YamlDotNet.Serialization;

namespace Recyclarr.Config.EnvironmentVariables;

public class EnvironmentVariablesYamlBehavior(IConfiguration configuration) : IYamlBehavior
{
    public void Setup(DeserializerBuilder builder)
    {
        builder
            .WithNodeDeserializer(new EnvironmentVariablesDeserializer(configuration))
            .WithTagMapping("!env_var", typeof(EnvironmentVariableTag));
    }
}
