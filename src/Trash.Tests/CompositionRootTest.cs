using System.Collections;
using System.Linq;
using Autofac;
using Autofac.Core;
using FluentAssertions;
using NUnit.Framework;

namespace Trash.Tests
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class CompositionRootTest
    {
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
                    .GetEnumerator();
            }
        }

        [TestCaseSource(typeof(ConcreteTypeEnumerator))]
        public void Resolve_ICommandConcreteClasses(Service service)
        {
            using var container = CompositionRoot.Setup();
            container.Invoking(c => c.ResolveService(service))
                .Should().NotThrow()
                .And.NotBeNull();
        }
    }
}
