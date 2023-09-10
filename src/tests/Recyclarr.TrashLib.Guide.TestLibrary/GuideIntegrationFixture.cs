using Autofac;
using Recyclarr.TrashLib.TestLibrary;
using Recyclarr.Yaml;

namespace Recyclarr.TrashLib.Guide.TestLibrary;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public abstract class GuideIntegrationFixture : TrashLibIntegrationFixture
{
    protected override void RegisterTypes(ContainerBuilder builder)
    {
        base.RegisterTypes(builder);
        builder.RegisterModule<GuideAutofacModule>();

        // dependencies
        builder.RegisterModule<YamlAutofacModule>();
    }
}
