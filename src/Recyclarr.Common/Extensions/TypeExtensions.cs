namespace Recyclarr.Common.Extensions;

public static class TypeExtensions
{
    public static bool IsGenericTypeOf(this Type type, Type genericType)
    {
        return type is {IsGenericType: true} && type.GetGenericTypeDefinition() == genericType;
    }

    public static bool IsImplementationOf(this Type type, Type collectionType)
    {
        return
            type is {IsInterface: true} && type.IsGenericTypeOf(collectionType) ||
            Array.Exists(type.GetInterfaces(), i => i.IsGenericTypeOf(typeof(ICollection<>)));
    }
}
