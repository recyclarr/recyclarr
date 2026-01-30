using Autofac;
using Autofac.Core;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Config.Models;
using Recyclarr.TestLibrary.Autofac;

namespace Recyclarr.Cli.Tests.IntegrationTests;

internal sealed class CompositionRootTest : CliIntegrationFixture
{
    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        base.RegisterStubsAndMocks(builder);
        builder.RegisterMockFor<IServiceConfiguration>();
    }

#pragma warning disable TUnit0046 // Type is immutable; isolation not needed
    public static IEnumerable<Type> GetServiceTypes()
#pragma warning restore TUnit0046
    {
        var builder = new ContainerBuilder();
        CompositionRoot.Setup(builder);
        var container = builder.Build();

        return container
            .ComponentRegistry.Registrations.SelectMany(x => x.Services)
            .OfType<TypedService>()
            .Select(x => x.ServiceType)
            .Distinct()
            .Where(x =>
                x.FullName == null || !x.FullName.StartsWith("Autofac.", StringComparison.Ordinal)
            )
            .Where(x => x.Name is not "GitProviderLocation" and not "LocalProviderLocation");
    }

    [Test]
    [MethodDataSource(nameof(GetServiceTypes))]
    public void Service_should_be_instantiable(Type service)
    {
        Container.Resolve(service).Should().NotBeNull();
    }
}
