using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Recyclarr.Yaml.YamlDotNet;

// from: https://github.com/aaubry/YamlDotNet/issues/236#issuecomment-632054372
public sealed class ReadOnlyCollectionNodeTypeResolver : INodeTypeResolver
{
    public bool Resolve(NodeEvent? nodeEvent, ref Type currentType)
    {
        if (!currentType.IsInterface || !currentType.IsGenericType ||
            !CustomGenericInterfaceImplementations.TryGetValue(currentType.GetGenericTypeDefinition(),
                out var concreteType))
        {
            return false;
        }

        currentType = concreteType.MakeGenericType(currentType.GetGenericArguments());
        return true;
    }

    private static readonly IReadOnlyDictionary<Type, Type> CustomGenericInterfaceImplementations =
        new Dictionary<Type, Type>
        {
            {typeof(IReadOnlyCollection<>), typeof(List<>)},
            {typeof(IReadOnlyList<>), typeof(List<>)},
            {typeof(IReadOnlyDictionary<,>), typeof(Dictionary<,>)}
        };
}
