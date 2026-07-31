using Autofac;
using Autofac.Extras.Ordering;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Pipelines;
using Recyclarr.ResourceProviders;

namespace Recyclarr.Core.Tests.Reusable;

internal abstract class CoreIntegrationTestFixture : IntegrationTestFixture
{
    protected override void RegisterTypes(ContainerBuilder builder)
    {
        builder.RegisterSource<OrderedRegistrationSource>();
        builder.RegisterModule<CoreAutofacModule>();
        builder.RegisterModule<PipelineAutofacModule>();
        builder.RegisterModule<ResourceProviderAutofacModule>();
    }
}
