using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Autofac;
using Autofac.Core;
using FluentAssertions;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Recyclarr.Cli.TestLibrary;

namespace Recyclarr.Cli.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CompositionRootTest
{
    // Warning CA1812 : CompositionRootTest.ConcreteTypeEnumerator is an internal class that is apparently never
    // instantiated.
    [SuppressMessage("Performance", "CA1812", Justification = "Created via reflection by TestCaseSource attribute")]
    private sealed class ConcreteTypeEnumerator : IntegrationFixture, IEnumerable
    {
        public IEnumerator GetEnumerator()
        {
            return Container.ComponentRegistry.Registrations
                .SelectMany(x => x.Services)
                .OfType<TypedService>()
                .Select(x => x.ServiceType)
                .Distinct()
                .Where(x => x.FullName == null || !x.FullName.StartsWith("Autofac."))
                .Select(x => new TestCaseParameters(new object[] {Container, x}) {TestName = x.FullName})
                .GetEnumerator();
        }
    }

    [TestCaseSource(typeof(ConcreteTypeEnumerator))]
    public void Service_should_be_instantiable(ILifetimeScope scope, Type service)
    {
        scope.Resolve(service).Should().NotBeNull();
    }
}
