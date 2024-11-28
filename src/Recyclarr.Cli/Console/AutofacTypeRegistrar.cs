using Autofac;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console;

internal class AutofacTypeRegistrar(ILifetimeScope scope) : ITypeRegistrar
{
    private readonly List<(Type, Type)> _typeRegistrations = [];
    private readonly List<(Type, object)> _instanceRegistrations = [];
    private readonly List<(Type, Func<object>)> _lazyRegistrations = [];

    public void Register(Type service, Type implementation)
    {
        _typeRegistrations.Add((service, implementation));
    }

    public void RegisterInstance(Type service, object implementation)
    {
        _instanceRegistrations.Add((service, implementation));
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        _lazyRegistrations.Add((service, factory));
    }

    public ITypeResolver Build()
    {
        return new AutofacTypeResolver(
            scope.BeginLifetimeScope(builder =>
            {
                foreach (var (service, impl) in _typeRegistrations)
                {
                    builder.RegisterType(impl).As(service).SingleInstance();
                }

                foreach (var (service, implementation) in _instanceRegistrations)
                {
                    builder.RegisterInstance(implementation).As(service);
                }

                foreach (var (service, factory) in _lazyRegistrations)
                {
                    builder.Register(_ => factory()).As(service).SingleInstance();
                }
            })
        );
    }
}
