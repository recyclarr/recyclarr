using Recyclarr.Common.Extensions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Recyclarr.Yaml.YamlDotNet;

// Borrowed from: https://github.com/aaubry/YamlDotNet/issues/443#issuecomment-544449498
public sealed class ForceEmptySequences(IObjectFactory objectFactory) : INodeDeserializer
{
    bool INodeDeserializer.Deserialize(
        IParser reader,
        Type expectedType,
        Func<IParser, Type, object?> nestedObjectDeserializer,
        out object? value,
        ObjectDeserializer rootDeserializer
    )
    {
        value = null;

        if (!IsList(expectedType) || !reader.Accept<NodeEvent>(out var evt) || !NodeIsNull(evt))
        {
            return false;
        }

        reader.SkipThisAndNestedEvents();
        value = objectFactory.Create(expectedType);
        return true;
    }

    private static bool NodeIsNull(NodeEvent nodeEvent)
    {
        // http://yaml.org/type/null.html

        if (nodeEvent.Tag == "tag:yaml.org,2002:null")
        {
            return true;
        }

        if (nodeEvent is not Scalar { Style: ScalarStyle.Plain } scalar)
        {
            return false;
        }

        var value = scalar.Value;
        return value is "" or "~" or "null" or "Null" or "NULL";
    }

    private static bool IsList(Type type)
    {
        return type.IsImplementationOf(typeof(ICollection<>))
            || type.IsImplementationOf(typeof(IReadOnlyCollection<>));
    }
}
