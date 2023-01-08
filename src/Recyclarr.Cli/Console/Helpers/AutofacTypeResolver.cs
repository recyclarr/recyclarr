using Autofac;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Helpers;

internal class AutofacTypeResolver : ITypeResolver
{
    private readonly ILifetimeScope _scope;

    public AutofacTypeResolver(ILifetimeScope scope)
    {
        _scope = scope;
    }

    public object? Resolve(Type? type)
    {
        return type is not null ? _scope.Resolve(type) : null;
    }
}
