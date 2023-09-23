using Autofac;
using Recyclarr.TrashGuide;
using Recyclarr.TrashLib.TestLibrary;
using Recyclarr.Yaml;

namespace Recyclarr.Config.TestLibrary;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public abstract class ConfigIntegrationFixture : TrashLibIntegrationFixture
{
    protected override void RegisterTypes(ContainerBuilder builder)
    {
        base.RegisterTypes(builder);
        builder.RegisterModule<ConfigAutofacModule>();

        // dependencies
        builder.RegisterModule<GuideAutofacModule>();
        builder.RegisterModule<YamlAutofacModule>();
    }
}
