using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;
using System.IO.Abstractions.TestingHelpers;
using Autofac;
using Autofac.Core;
using CliFx.Infrastructure;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Recyclarr.Command;
using Serilog;
using TrashLib;
using TrashLib.Config.Services;
using TrashLib.Repo.VersionControl;
using TrashLib.Startup;

namespace Recyclarr.Tests;

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

        // Do not use `NotThrow()` here because fluent assertions doesn't show the full exception details
        // See: https://github.com/fluentassertions/fluentassertions/issues/2015
        act(); //.Should().NotThrow();
    }

    // Warning CA1812 : CompositionRootTest.ConcreteTypeEnumerator is an internal class that is apparently never
    // instantiated.
    [SuppressMessage("Performance", "CA1812",
        Justification = "Created via reflection by TestCaseSource attribute"
    )]
    private sealed class ConcreteTypeEnumerator : IEnumerable
    {
        private readonly ILifetimeScope _container;

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

    private static void RegisterAdditionalServices(ContainerBuilder builder)
    {
        var fs = new MockFileSystem();
        builder.RegisterInstance(fs).As<IFileSystem>();
        builder.RegisterInstance(new AppPaths(fs.CurrentDirectory())).As<IAppPaths>();
        builder.RegisterInstance(Substitute.For<IConsole>());
        builder.RegisterInstance(Substitute.For<ILogger>());
        builder.RegisterInstance(Substitute.For<IServiceCommand>());
        builder.RegisterInstance(Substitute.For<IServiceConfiguration>());
    }

    [TestCaseSource(typeof(ConcreteTypeEnumerator))]
    public void Service_should_be_instantiable(Type service)
    {
        using var container = CompositionRoot.Setup(RegisterAdditionalServices);
        container.Resolve(service).Should().NotBeNull();
    }
}
