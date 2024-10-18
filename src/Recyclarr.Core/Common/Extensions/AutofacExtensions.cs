using Autofac;
using Autofac.Builder;

namespace Recyclarr.Common.Extensions;

public static class AutofacExtensions
{
    public static object ResolveGeneric(this ILifetimeScope scope, Type genericType, params Type[] genericArgs)
    {
        var type = genericType.MakeGenericType(genericArgs);
        return scope.Resolve(type);
    }

    public static IRegistrationBuilder<TLimit, TReflectionActivatorData, TStyle>
        WithTypeParameter<TLimit, TReflectionActivatorData, TStyle>(
            this IRegistrationBuilder<TLimit, TReflectionActivatorData, TStyle> builder,
            Type paramType,
            Func<IComponentContext, object> resolver)
        where TReflectionActivatorData : ReflectionActivatorData
    {
        return builder.WithParameter(
            (info, _) => info.ParameterType == paramType,
            (_, context) => resolver(context));
    }
}
