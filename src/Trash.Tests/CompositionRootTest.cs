using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Autofac;
using CliFx;
using NUnit.Framework;

namespace Trash.Tests
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class CompositionRootTest
    {
        private class ConcreteTypeEnumerator<T> : IEnumerable
        {
            private readonly Assembly _asm;

            public ConcreteTypeEnumerator()
            {
                _asm = Assembly.GetAssembly(typeof(CompositionRoot)) ?? throw new NullReferenceException();
            }

            public IEnumerator GetEnumerator()
            {
                return _asm.DefinedTypes
                    .Where(t => t.GetInterfaces().Contains(typeof(ICommand)) && !t.IsAbstract)
                    .GetEnumerator();
            }
        }

        [TestCaseSource(typeof(ConcreteTypeEnumerator<ICommand>))]
        public void Resolve_ICommandConcreteClasses(Type concreteCmd)
        {
            var builder = new ContainerBuilder();
            var container = CompositionRoot.Setup(builder);
            container.Resolve(concreteCmd);
        }
    }
}
