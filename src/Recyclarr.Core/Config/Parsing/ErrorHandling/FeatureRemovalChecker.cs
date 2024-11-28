using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Recyclarr.Config.Parsing.ErrorHandling;

// Note: Backward breaking changes involving node removals cannot be handled here, since that will cause exceptions
// before the Node Type Resolver gets invoked. Those are handled reactively by inspecting the YamlException object
// passed to the ContextualMessages static class.
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
                "https://recyclarr.dev/wiki/upgrade-guide/v5.0/#instances-must-now-be-named"
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
