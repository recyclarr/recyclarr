using Autofac;

namespace Recyclarr.Common.Extensions;

public static class AutofacExtensions
{
    public static object ResolveGeneric(this ILifetimeScope scope, Type genericType, params Type[] genericArgs)
    {
        var type = genericType.MakeGenericType(genericArgs);
        return scope.Resolve(type);
    }
}
