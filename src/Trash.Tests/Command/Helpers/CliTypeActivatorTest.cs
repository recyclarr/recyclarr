using Autofac;
using FluentAssertions;
using NUnit.Framework;
using Trash.Command;
using Trash.Command.Helpers;

namespace Trash.Tests.Command.Helpers;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CliTypeActivatorTest
{
    private class NonServiceCommandType
    {
    }

    private class StubCommand : IServiceCommand
    {
        public bool Preview => false;
        public bool Debug => false;
        public ICollection<string>? Config => null;
        public string CacheStoragePath => "";
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
