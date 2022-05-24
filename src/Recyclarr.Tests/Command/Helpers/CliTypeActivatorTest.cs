using System.Diagnostics.CodeAnalysis;
using Autofac;
using FluentAssertions;
using NUnit.Framework;
using Recyclarr.Command;
using Recyclarr.Command.Helpers;

namespace Recyclarr.Tests.Command.Helpers;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CliTypeActivatorTest
{
    // Warning CA1812 : an internal class that is apparently never instantiated.
    [SuppressMessage("Performance", "CA1812", Justification = "Registered to and created by Autofac")]
    private class NonServiceCommandType
    {
    }

    // Warning CA1812 : an internal class that is apparently never instantiated.
    [SuppressMessage("Performance", "CA1812", Justification = "Registered to and created by Autofac")]
    private class StubCommand : IServiceCommand
    {
        public bool Preview => false;
        public bool Debug => false;
        public ICollection<string> Config => new List<string>();
        public string CacheStoragePath => "";
        public string Name => "";
    }

    [Test]
    public void Resolve_NonServiceCommandType_NoActiveCommandSet()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<NonServiceCommandType>();
        var container = CompositionRoot.Setup(builder);

        var createdType = CliTypeActivator.ResolveType(container, typeof(NonServiceCommandType));

        Action act = () => _ = container.Resolve<IActiveServiceCommandProvider>().ActiveCommand;

        createdType.Should().BeOfType<NonServiceCommandType>();
        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("The active command has not yet been determined");
    }

    [Test]
    public void Resolve_ServiceCommandType_ActiveCommandSet()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<StubCommand>();
        var container = CompositionRoot.Setup(builder);

        var createdType = CliTypeActivator.ResolveType(container, typeof(StubCommand));
        var activeCommand = container.Resolve<IActiveServiceCommandProvider>().ActiveCommand;

        activeCommand.Should().BeSameAs(createdType);
        activeCommand.Should().BeOfType<StubCommand>();
    }
}
