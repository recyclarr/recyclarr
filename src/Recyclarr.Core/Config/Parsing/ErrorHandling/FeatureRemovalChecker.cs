using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Recyclarr.Config.Parsing.ErrorHandling;

// Note: Backward breaking changes involving property removals are handled by
// DeprecatedPropertyInspector, not here. This checker only detects structural changes
// (e.g. array-style instances) that are visible at the node type resolver level.
public sealed class FeatureRemovalChecker : INodeTypeResolver
{
    public bool Resolve(NodeEvent? nodeEvent, ref Type currentType)
    {
        if (
            IsDictionaryOfType(currentType, typeof(RadarrConfigYaml), typeof(SonarrConfigYaml))
            && nodeEvent is SequenceStart
        )
        {
            throw new FeatureRemovalException(
                "Found array-style list of instances instead of named-style. "
                    + "Array-style lists of Sonarr/Radarr instances are not supported.",
                "https://recyclarr.dev/guide/upgrade-guide/v5.0/#instances-must-now-be-named"
            );
        }

        return false;
    }

    private static bool IsDictionaryOfType(Type dictType, params Type[] valueTypes)
    {
        if (
            !dictType.IsGenericType
            || dictType.GetGenericTypeDefinition() != typeof(IReadOnlyDictionary<,>)
        )
        {
            return false;
        }

        return valueTypes.Contains(dictType.GenericTypeArguments[1]);
    }
}
