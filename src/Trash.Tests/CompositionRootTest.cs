using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Core;
using FluentAssertions;
using NUnit.Framework;
using VersionControl;

namespace Trash.Tests;

public record ServiceFactoryWrapper(Type Service, Action<ILifetimeScope> Instantiate);

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CompositionRootTest
{
    private static class FactoryForService<TService>
    {
        public static ServiceFactoryWrapper WithArgs<TP1>(TP1 arg1 = default!)
        {
            return new ServiceFactoryWrapper(typeof(TService),
                c => c.Resolve<Func<TP1, TService>>().Invoke(arg1));
        }
    }

    private static readonly List<ServiceFactoryWrapper> FactoryTests = new()
    {
        FactoryForService<IGitRepository>.WithArgs("path")
    };

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
                .Where(x => FactoryTests.All(y => y.Service != x.ServiceType))
                .GetEnumerator();
        }
    }

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

    [TestCaseSource(typeof(ConcreteTypeEnumerator))]
    public void Service_should_be_instantiable(Service service)
    {
        using var container = CompositionRoot.Setup();
        container.Invoking(c => c.ResolveService(service))
            .Should().NotThrow()
            .And.NotBeNull();
    }
}
