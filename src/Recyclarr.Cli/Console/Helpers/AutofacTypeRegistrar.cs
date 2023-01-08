using Autofac;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Helpers;

internal class AutofacTypeRegistrar : ITypeRegistrar
{
    private readonly ContainerBuilder _builder;
    private readonly Action<ILifetimeScope> _assignScope;

    public AutofacTypeRegistrar(ContainerBuilder builder, Action<ILifetimeScope> assignScope)
    {
        _builder = builder;
        _assignScope = assignScope;
    }

    public void Register(Type service, Type implementation)
    {
        _builder.RegisterType(implementation).As(service).SingleInstance();
    }

    public void RegisterInstance(Type service, object implementation)
    {
        _builder.RegisterInstance(implementation).As(service);
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        _builder.Register(_ => factory()).As(service).SingleInstance();
    }

    public ITypeResolver Build()
    {
        var container = _builder.Build();
        _assignScope(container);
        return new AutofacTypeResolver(container);
    }
}
