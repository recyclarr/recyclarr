using Autofac;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectFactories;

namespace Recyclarr.Yaml;

public class YamlAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<YamlSerializerFactory>().As<IYamlSerializerFactory>();
        builder.RegisterType<DefaultObjectFactory>().As<IObjectFactory>();
    }
}
