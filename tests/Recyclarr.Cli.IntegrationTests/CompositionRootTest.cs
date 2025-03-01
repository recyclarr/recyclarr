using System.Collections;
using Autofac;
using Autofac.Core;
using NUnit.Framework.Internal;
using Recyclarr.Config.Models;
using Recyclarr.Platform;
using Recyclarr.TestLibrary.Autofac;
using Spectre.Console;

namespace Recyclarr.Cli.IntegrationTests;

[TestFixture]
public class CompositionRootTest
{
    [UsedImplicitly]
    private sealed class ConcreteTypeEnumerator : IEnumerable
    {
        public IEnumerator GetEnumerator()
        {
            var builder = new ContainerBuilder();
            CompositionRoot.Setup(builder);

            // These are things that Spectre.Console normally registers for us, so they won't explicitly be
            // in the CompositionRoot. Register mocks/stubs here.
            builder.RegisterMockFor<IAnsiConsole>();

            // Normally in per-instance syncing, a child lifetime scope is created to register IServiceConfiguration.
            // However, in the test for checking whether all necessary dependencies are registered, we provide a mock
            // registration here for the purposes of getting the test to pass.
            builder.RegisterMockFor<IServiceConfiguration>();

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
                .Select(x => new TestCaseParameters([container, x]) { TestName = x.FullName })
                .GetEnumerator();
        }
    }

    [TestCaseSource(typeof(ConcreteTypeEnumerator))]
    public void Service_should_be_instantiable(ILifetimeScope scope, Type service)
    {
        // Required to bypass exception due to the directory override being null
        scope.Resolve<IAppDataSetup>().SetAppDataDirectoryOverride("");

        scope.Resolve(service).Should().NotBeNull();
    }
}
