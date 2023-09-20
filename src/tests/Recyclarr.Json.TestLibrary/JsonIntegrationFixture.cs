using Autofac;
using Recyclarr.TestLibrary;

namespace Recyclarr.Json.TestLibrary;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public abstract class JsonIntegrationFixture : IntegrationTestFixture
{
    protected override void RegisterTypes(ContainerBuilder builder)
    {
        base.RegisterTypes(builder);
        builder.RegisterModule<JsonAutofacModule>();
    }
}
