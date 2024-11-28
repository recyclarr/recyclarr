using System.Reflection;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Recyclarr.Yaml.YamlDotNet;

public sealed class SyntaxErrorHelper : INodeTypeResolver
{
    private static readonly string[] CollectionKeywords = ["Collection", "List"];

    public bool Resolve(NodeEvent? nodeEvent, ref Type currentType)
    {
        CheckSequenceAssignedToNonSequence(nodeEvent, currentType);
        return false;
    }

    // If the user tries to specify an array as the value for a node type that is not a list type, then we provide our
    // own exception type. The default error message that YamlDotNet would output doesn't make much sense to users: It
    // just says "no node type resolver could resolve the type", or something along those lines -- which isn't helpful!
    private static void CheckSequenceAssignedToNonSequence(
        ParsingEvent? nodeEvent,
        MemberInfo currentType
    )
    {
        if (
            nodeEvent is SequenceStart
            && !Array.Exists(
                CollectionKeywords,
                x => currentType.Name.Contains(x, StringComparison.Ordinal)
            )
        )
        {
            throw new YamlException(
                nodeEvent.Start,
                nodeEvent.End,
                $"A list/array/sequence is not allowed for {currentType.Name}"
            );
        }
    }
}
