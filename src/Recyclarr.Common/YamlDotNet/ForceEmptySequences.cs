using System.Collections;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Recyclarr.Common.YamlDotNet;

// Borrowed from: https://github.com/aaubry/YamlDotNet/issues/443#issuecomment-544449498
public sealed class ForceEmptySequences : INodeDeserializer
{
    private readonly IObjectFactory _objectFactory;

    public ForceEmptySequences(IObjectFactory objectFactory)
    {
        _objectFactory = objectFactory;
    }

    bool INodeDeserializer.Deserialize(
        IParser reader,
        Type expectedType,
        Func<IParser, Type, object?> nestedObjectDeserializer,
        out object? value)
    {
        value = null;

        if (!IsEnumerable(expectedType) || !reader.Accept<NodeEvent>(out var evt) || !NodeIsNull(evt))
        {
            return false;
        }

        reader.SkipThisAndNestedEvents();
        value = _objectFactory.Create(expectedType);
        return true;
    }

    private static bool NodeIsNull(NodeEvent nodeEvent)
    {
        // http://yaml.org/type/null.html

        if (nodeEvent.Tag == "tag:yaml.org,2002:null")
        {
            return true;
        }

        if (nodeEvent is not Scalar {Style: ScalarStyle.Plain} scalar)
        {
            return false;
        }

        var value = scalar.Value;
        return value is "" or "~" or "null" or "Null" or "NULL";
    }

    private static bool IsEnumerable(Type type)
    {
        return typeof(IEnumerable).IsAssignableFrom(type);
    }
}
