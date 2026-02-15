using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Autofac;
using Autofac.Core;
using NUnit.Framework.Internal;
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

    [SuppressMessage(
        "Microsoft.Performance",
        "CA1812:AvoidUninstantiatedInternalClasses",
        Justification = "Instantiated via TestCaseSource"
    )]
    private sealed class ConcreteTypeEnumerator : IEnumerable
    {
        public IEnumerator GetEnumerator()
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
                    x.FullName == null
                    || !x.FullName.StartsWith("Autofac.", StringComparison.Ordinal)
                )
                .Where(x => x.Name is not "GitProviderLocation" and not "LocalProviderLocation")
                .Select(x => new TestCaseParameters([x]) { TestName = x.FullName })
                .GetEnumerator();
        }
    }

    // Resolve from a "sync" child scope so that sync-scoped types are resolvable.
    // Child scopes can also resolve parent (root) registrations, so this covers everything.
    [TestCaseSource(typeof(ConcreteTypeEnumerator))]
    public void Service_should_be_instantiable(Type service)
    {
        using var syncScope = Container.BeginLifetimeScope("sync");
        syncScope.Resolve(service).Should().NotBeNull();
    }
}
