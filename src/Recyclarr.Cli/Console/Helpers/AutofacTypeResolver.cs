using Autofac;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Helpers;

internal class AutofacTypeResolver(ILifetimeScope scope) : ITypeResolver
{
    public object? Resolve(Type? type)
    {
        return type is not null ? scope.Resolve(type) : null;
    }
}
