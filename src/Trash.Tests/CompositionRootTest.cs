using System.Collections;
using Autofac;
using Autofac.Core;
using FluentAssertions;
using NUnit.Framework;
using VersionControl;

namespace Trash.Tests;

public record ServiceFactoryWrapper(Type Service, Action<ILifetimeScope> Instantiate);

public static class FactoryForService<TService>
{
    public static ServiceFactoryWrapper WithArgs<TP1>(TP1 arg1 = default!)
    {
        return new ServiceFactoryWrapper(typeof(TService),
            c => c.Resolve<Func<TP1, TService>>().Invoke(arg1));
    }
}

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CompositionRootTest
{
    private static readonly List<ServiceFactoryWrapper> FactoryTests = new()
    {
        FactoryForService<IGitRepository>.WithArgs("path")
    };

    [TestCaseSource(typeof(CompositionRootTest), nameof(FactoryTests))]
    public void Service_requiring_factory_should_be_instantiable(ServiceFactoryWrapper service)
    {
        var act = () =>
        {
            using var container = CompositionRoot.Setup();
            service.Instantiate(container);
        };

        act.Should().NotThrow();
    }

    private sealed class ConcreteTypeEnumerator : IEnumerable
    {
        private readonly IContainer _container;

        public ConcreteTypeEnumerator()
        {
            _container = CompositionRoot.Setup();
        }

        public IEnumerator GetEnumerator()
        {
            return _container.ComponentRegistry.Registrations
                .SelectMany(x => x.Services)
                .OfType<TypedService>()
                .Select(x => x.ServiceType)
                .Distinct()
                .Except(FactoryTests.Select(x => x.Service))
                .Where(x => x.FullName == null || !x.FullName.StartsWith("Autofac."))
                .GetEnumerator();
        }
    }

    [TestCaseSource(typeof(ConcreteTypeEnumerator))]
    public void Service_should_be_instantiable(Type service)
    {
        using var container = CompositionRoot.Setup();
        container.Invoking(c => c.Resolve(service))
            .Should().NotThrow()
            .And.NotBeNull();
    }
}
